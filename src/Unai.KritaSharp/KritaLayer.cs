using System;
using System.Numerics;
using System.Xml;

namespace Unai.KritaSharp;

public class KritaLayer
{
	public string Name { get; set; }
	public string Filename { get; set; }
	public Guid Uuid { get; set; }
	public bool Locked { get; set; }
	public bool Visible { get; set; } = true;
	public bool OnionSkin { get; set; }
	public bool Collapsed { get; set; }
	public bool InTimeline { get; set; } = false;
	public float Opacity { get; set; } = 1f;
	public int OffsetX { get; set; }
	public int OffsetY { get; set; }

	public void LoadFromKritaXml(XmlElement layerXmlElem)
	{
		Name = layerXmlElem.GetAttribute("name");
		Filename = layerXmlElem.GetAttribute("filename");
		Uuid = new(layerXmlElem.GetAttribute("uuid"));
		Locked = layerXmlElem.GetAttribute("locked") == "1";
		Visible = layerXmlElem.GetAttribute("visible") == "1";
		OnionSkin = layerXmlElem.GetAttribute("onionskin") == "1";
		Collapsed = layerXmlElem.GetAttribute("collapsed") == "1";
		InTimeline = layerXmlElem.GetAttribute("intimeline") == "1";
		Opacity = int.Parse(layerXmlElem.GetAttribute("opacity")) / 255f; // FIXME: Shouldn't this be 256?
		OffsetX = int.Parse(layerXmlElem.GetAttribute("x"));
		OffsetY = int.Parse(layerXmlElem.GetAttribute("y"));
	}
}