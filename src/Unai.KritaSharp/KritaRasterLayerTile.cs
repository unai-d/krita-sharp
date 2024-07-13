namespace Unai.KritaSharp;

public class KritaRasterLayerTile
{
	public int OffsetX { get; internal set; }
	public int OffsetY { get; internal set; }
	public byte[] Data { get; internal set; }

	internal KritaRasterLayerTile(int offsetX, int offsetY, byte[] data)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
		Data = data;
	}
}