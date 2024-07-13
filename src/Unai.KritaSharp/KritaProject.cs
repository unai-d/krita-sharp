using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Unai.KritaSharp;

public class KritaProject : IDisposable
{
	ZipArchive _baseZip;
	XmlDocument _mainDocXml;
	Dictionary<Guid, KritaRasterLayer> _rasterLayerCache = []; // TODO: Make it static perhaps?

	#region Data from `mainDoc.xml`

	public string EditorName { get; set; }
	public Version? KritaVersion { get; set; }
	public string ProjectName { get; set; }
	public int CanvasWidth { get; set; }
	public int CanvasHeight { get; set; }
	public string ColorProfileFileName { get; set; }
	public string ProjectDescription { get; set; }

	#endregion
	public List<KritaLayer> Layers { get; } = [];

	public ZipArchive BaseZipArchive => _baseZip;
	public Stream PreviewPngStream => _baseZip.GetEntry("preview.png")?.Open() ?? throw new FileNotFoundException("Preview data not available.");
	public Stream FinalRenderPngStream => _baseZip.GetEntry("mergedimage.png")?.Open() ?? throw new FileNotFoundException("Final render data not available. Notice that backup and auto-saved projects don't store this data.");

	public KritaProject(string path, FileAccess fileAccess = FileAccess.Read)
	{
		_baseZip = new(File.Open(path, FileMode.Open, fileAccess), fileAccess.HasFlag(FileAccess.Write) ? ZipArchiveMode.Update : ZipArchiveMode.Read);
		Load();
	}

	public KritaProject(Stream stream)
	{
		_baseZip = new(stream, ZipArchiveMode.Update);
		Load();
	}

	public void Dispose()
	{
		_baseZip.Dispose();
	}

	public bool IsMimeTypeValid()
	{
		var mimetypeFileStr = _baseZip.GetEntry("mimetype").Open();
		return new StreamReader(mimetypeFileStr).ReadToEnd() == "application/x-krita";
	}

	private void Load()
	{
		_mainDocXml = new();
		_mainDocXml.Load(_baseZip.GetEntry("maindoc.xml").Open());

		var docXmlElem = _mainDocXml.DocumentElement;
		EditorName = docXmlElem.GetAttribute("editor");
		KritaVersion = new(docXmlElem.GetAttribute("kritaVersion").Split('-')[0]); // TODO: Better version parsing.

		var imageXmlElem = (XmlElement)docXmlElem.GetElementsByTagName("IMAGE")[0];
		CanvasWidth = int.Parse(imageXmlElem.GetAttribute("width"));
		CanvasHeight = int.Parse(imageXmlElem.GetAttribute("height"));
		ProjectName = imageXmlElem.GetAttribute("name");
		ProjectDescription = imageXmlElem.GetAttribute("description");
		ColorProfileFileName = imageXmlElem.GetAttribute("profile");

		var layersXmlElem = (XmlElement)imageXmlElem.GetElementsByTagName("layers")[0];
		foreach (var layerXmlElem in layersXmlElem.ChildNodes.OfType<XmlElement>())
		{
			var layer = new KritaLayer();
			layer.LoadFromKritaXml(layerXmlElem);
			Layers.Add(layer);
		}
	}

	public KritaLayer GetLayerByUuid(Guid uuid)
	{
		return Layers.First(l => l.Uuid == uuid);
	}

	public KritaRasterLayer GetRasterLayer(KritaLayer layer)
	{
		if (_rasterLayerCache.TryGetValue(layer.Uuid, out var rlayer))
		{
			return rlayer;
		}

		string rasterDataPath = $"{ProjectName}/layers/{layer.Filename}";

		using var rasterDataDeflateStr = _baseZip.GetEntry(rasterDataPath).Open();
		using var rasterDataMemStr = new MemoryStream();
		rasterDataDeflateStr.CopyTo(rasterDataMemStr);
		rasterDataMemStr.Position = 0;

		var ret = new KritaRasterLayer();
		ret.LoadFromRasterFile(rasterDataMemStr);
		_rasterLayerCache.Add(layer.Uuid, ret);
		return ret;
	}
}
