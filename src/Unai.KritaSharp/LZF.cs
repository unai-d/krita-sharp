using System.IO;

namespace Unai.KritaSharp;

public class LZF
{
	public static void Decompress(Stream input, Stream output)
	{
		var inputLimit = input.Length - 1;

		while (input.Position < inputLimit)
		{
			int ctrl = input.ReadByte();
			// Console.Error.WriteLine($"LZF I{input.Position-1,8}→O{output.Position,8} op={ctrl:x2}");

			if (ctrl < 32)
			{
				ctrl++;
				for (; ctrl > 0; ctrl--)
				{
					output.WriteByte((byte)input.ReadByte());
				}
			}
			else
			{
				int ofs = (ctrl & 0x1f) << 8;
				int len = ctrl >> 5;
				var bref = output.Position - ofs - 1;

				var outPos = output.Position;

				if (len == 7)
				{
					len += input.ReadByte();
				}

				bref -= input.ReadByte();

				//Console.Error.WriteLine($"LZF ofs={ofs} len={len} bref={bref}");

				for (int i = 0; i < 2 + len; i++)
				{
					output.Position = bref + i;
					var brefVal = (byte)output.ReadByte();
					output.Position = outPos + i;
					output.WriteByte(brefVal);
				}
			}
		}
	}
}