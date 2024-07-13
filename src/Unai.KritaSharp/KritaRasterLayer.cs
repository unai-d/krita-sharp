using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SkiaSharp;

namespace Unai.KritaSharp;

public class KritaRasterLayer
{
	private readonly List<KritaRasterLayerTile> _tiles = [];

	public int BytesPerPixel { get; internal set; }
	public int TileWidth { get; internal set; }
	public int TileHeight { get; internal set; }
	public int TileFirstX { get; internal set; }
	public int TileFirstY { get; internal set; }
	public int TileLastX { get; internal set; }
	public int TileLastY { get; internal set; }
	public int LayerWidth { get; internal set; }
	public int LayerHeight { get; internal set; }

	internal void LoadFromRasterFile(Stream kraRasterFile)
	{
		_tiles.Clear();

		using BinaryReader br = new(kraRasterFile, Encoding.ASCII);

		int tileCount = 0;

		for (int i = 0; i < 16; i++)
		{
			string text = br.ReadStringUntilCharacter(0xa);
			string[] textFrags = text.Split(' ');

			switch (textFrags[0])
			{
				case "TILEWIDTH":
					TileWidth = int.Parse(textFrags[1]);
					break;

				case "TILEHEIGHT":
					TileHeight = int.Parse(textFrags[1]);
					break;

				case "PIXELSIZE":
					BytesPerPixel = int.Parse(textFrags[1]);
					break;

				case "DATA":
					tileCount = int.Parse(textFrags[1]);
					break;
			}

			if (tileCount > 0)
			{
				break;
			}
		}

		int tilePixelDataSize = TileWidth * TileHeight * BytesPerPixel;

		int minWidth = int.MaxValue,
			maxWidth = int.MinValue,
			minHeight = int.MaxValue,
			maxHeight = int.MinValue;

		for (int tileIdx = 0; tileIdx < tileCount; tileIdx++)
		{
			string tileHeader = br.ReadStringUntilCharacter(0xa);
			string[] tileHeaderParams = tileHeader.Split(',');
			int tileX = int.Parse(tileHeaderParams[0]);
			int tileY = int.Parse(tileHeaderParams[1]);
			string tileCompressionMethod = tileHeaderParams[2];
			int tileDataSize = int.Parse(tileHeaderParams[3]);

			bool isCompressed = (br.ReadByte() | 1) == 1; // This byte is a flag, so OR operator.
			byte[] tileData = br.ReadBytes(tileDataSize - 1);

			// Guess the layer's width and height.
			if (minWidth > tileX)
			{
				minWidth = tileX;
			}

			if (maxWidth < tileX)
			{
				maxWidth = tileX;
			}

			if (minHeight > tileY)
			{
				minHeight = tileY;
			}

			if (maxHeight < tileY)
			{
				maxHeight = tileY;
			}

			using MemoryStream tileDataIn = new(tileData);
			using MemoryStream tileDataOut = new();

			if (isCompressed)
			{
				LZF.Decompress(tileDataIn, tileDataOut);
			}
			else
			{
				tileDataIn.CopyTo(tileDataOut);
			}

			_tiles.Add(new(tileX, tileY, tileDataOut.ToArray()));
		}

		LayerWidth = maxWidth - minWidth + TileWidth;
		LayerHeight = maxHeight - minHeight + TileHeight;
		TileFirstX = minWidth;
		TileFirstY = minHeight;
		TileLastX = maxWidth;
		TileLastY = maxHeight;
	}

	public byte[] GetPixelData()
	{
		byte[] ret = new byte[LayerWidth * LayerHeight * BytesPerPixel];
		Console.Error.WriteLine($"{LayerWidth}×{LayerHeight}×{BytesPerPixel} = {ret.Length} bytes");

		foreach (KritaRasterLayerTile tile in _tiles)
		{
			int tileOffsetX = tile.OffsetX - TileFirstX;
			int tileOffsetY = tile.OffsetY - TileFirstY;

			if (tileOffsetX < 0 || tileOffsetY < 0)
			{
				Console.Error.WriteLine($"bad tile offsets: {tileOffsetX} {tileOffsetY}");
				continue;
			}

			// TODO: Optimize.
			for (int y = 0; y < TileHeight; y++)
			{
				for (int x = 0; x < TileWidth; x++)
				{
					for (int c = 0; c < BytesPerPixel; c++)
					{
						int source = x + (y * TileWidth) + (c * TileWidth * TileHeight);
						int destination = c + ((x + tileOffsetX + ((y + tileOffsetY) * LayerWidth)) * BytesPerPixel);

						if (destination < 0 || destination >= ret.Length)
						{
							Console.Error.WriteLine($"bad dest: {source} → {destination}");
							continue;
						}
						if (source >= tile.Data.Length)
						{
							Console.Error.WriteLine($"bad source: {source}");
							continue;
						}
						ret[destination] = tile.Data[source];
					}
				}
			}
		}

		return ret;
	}

	public byte[] GetAsBmp()
	{
		using MemoryStream ms = new();
		using BinaryWriter bw = new(ms);

		bw.Write(Encoding.ASCII.GetBytes("BM")); // Magic number.
		bw.Write(0); // File size (revisited later).
		bw.Write(0); // Reserved (always 0).
		bw.Write(0); // Data offset (revisited later).
		bw.Write(40); // Info header size.
		bw.Write(LayerWidth); // Pixel width.
		bw.Write(LayerHeight); // Pixel height.
		bw.Write((short)1); // Num. of planes.
		bw.Write((short)32); // Bits per pixel.
		bw.Write(0); // Compression type.
		bw.Write(0); // Image size when compressed.
		bw.Write(0); // TODO: Horizontal pixels per meter.
		bw.Write(0); // TODO: Vertical pixels per meter.
		bw.Write(0); // Num. of used colors.
		bw.Write(0); // Num. of important colors.
		var rasterDataPtr = ms.Position;
		bw.Write(Utils.InvertVertically(GetPixelData(), LayerWidth * BytesPerPixel)); // Raster data.

		ms.Position = 0x02; // File size.
		bw.Write((int)ms.Length);
		ms.Position = 0x0a; // Data offset.
		bw.Write((int)rasterDataPtr);

		return ms.ToArray();
	}

	public SKBitmap GetAsSkiaSharpBitmap()
	{
		var pixelData = GetPixelData();
		var bitmap = new SKBitmap(LayerWidth, LayerHeight, SKColorType.Bgra8888, SKAlphaType.Opaque);
		unsafe
		{
			fixed (byte* pixelDataPtr = pixelData)
			{
				nint destination = bitmap.GetPixels();
				bitmap.SetPixels((nint)pixelDataPtr);
			}
		}
		return bitmap;
	}

	public byte[] GetAsPng()
	{
		return GetAsSkiaSharpBitmap().Encode(SKEncodedImageFormat.Png, 100).ToArray();
	}
}