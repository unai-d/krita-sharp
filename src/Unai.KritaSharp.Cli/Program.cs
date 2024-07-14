using System;
using System.IO;
using Unai.KritaSharp;

class Program
{
	static readonly string _usageString = @"KritaSharp CLI – Tool for reading Krita project files
Copyright © Unai Domínguez 2024 – Licensed under MIT

Usage: Unai.KritaSharp.Cli <command> [<parameters>…]

Commands:
	d/details <input_file>
		Write a human-readable description of the <input_file> to the standard output.
	
	x/extract <input_file> <layer_uuid> [<output_file>]
		Extract the pixel data from layer with UUID <layer_uuid> and write it as a BMP file to the specified <output_file>.
		Note: if <output_file> is omitted, the result will be written to the standard output.
		Options:
			-f/--format <image_format>
				Set the image format of the output file. Default: `bmp`.
			-q/--quality <percentage>
				Set the quality of the output file. Some encoders are lossless and omit this parameter.
";

	static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.Error.WriteLine("Subcommand parameter is missing.");
			Console.Error.WriteLine(_usageString);
			return;
		}

		string command = args[0].ToLowerInvariant();

		switch (command)
		{
			case "-h":
			case "--help":
			case "-?":
			case "help":
				Console.Error.WriteLine(_usageString);
				return;

			case "d":
			case "details":
				DescribeProject(new KritaProject(args[1]));
				break;

			case "x":
			case "extract":
				ExtractLayer(args);
				break;

			default:
				Console.Error.WriteLine($"Unknown sub-command: {args[0]}");
				Console.Error.WriteLine(_usageString);
				break;
		}
	}

	static void DescribeProject(KritaProject kra)
	{
		Console.WriteLine($"Project name: {kra.ProjectName}");
		Console.WriteLine($"Created with: {kra.EditorName} {kra.KritaVersion}");
		Console.WriteLine($"Canvas size: {kra.CanvasWidth}×{kra.CanvasHeight}");

		foreach (var layer in kra.Layers)
		{
			Console.WriteLine($"Layer: {layer.Uuid} '{layer.Name}'");
			Console.WriteLine($"       Opacity {(int)(layer.Opacity*100)}%  Offset X={layer.OffsetX} Y={layer.OffsetY}");
		}
	}

	static void ExtractLayer(string[] args)
	{
		var kra = new KritaProject(args[1]);
		var layer = kra.GetLayerByUuid(Guid.Parse(args[2]));
		var rlayer = kra.GetRasterLayer(layer);

		string kraPath = null;
		ImageFormat outputFormat = ImageFormat.Bmp;
		int? outputQuality = null;

		for (int argi = 3; argi < args.Length; argi++)
		{
			var arg = args[argi];

			switch (arg)
			{
				default:
					if (kraPath == null)
					{
						kraPath = arg;
					}
					else
					{
						Console.Error.WriteLine($"Error: more than two output files specified.");
					}
					break;

				case "-f":
				case "--format":
					outputFormat = Enum.Parse<ImageFormat>(args[++argi], true);
					break;

				case "-q":
				case "--quality":
					outputQuality = int.Parse(args[++argi]);
					break;
			}
		}

		Stream outputStream = kraPath != null ? File.OpenWrite(kraPath) : Console.OpenStandardOutput();
		outputStream.Write(rlayer.GetAsImage(outputFormat, outputQuality));
	}
}
