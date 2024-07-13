using System;
using System.IO;
using System.Text;

namespace Unai.KritaSharp;

public static class Utils
{
	public static string ReadStringUntilCharacter(this BinaryReader br, byte terminator = 0)
	{
		var startPos = br.BaseStream.Position;
		int length = 0;

		while (br.BaseStream.ReadByte() != terminator)
		{
			length++;
		}

		br.BaseStream.Position = startPos;
		var ret = Encoding.ASCII.GetString(br.ReadBytes(length));
		br.BaseStream.Position++;
		return ret;
	}

	public static byte[] InvertVertically(byte[] pixelData, int stride)
	{
		byte[] ret = new byte[pixelData.Length];

		for (int inputPtr = 0; inputPtr < pixelData.Length; inputPtr += stride)
		{
			var outputPtr = ret.Length - inputPtr - stride;
			Array.Copy(pixelData, inputPtr, ret, outputPtr, stride);
		}

		return ret;
	}
}