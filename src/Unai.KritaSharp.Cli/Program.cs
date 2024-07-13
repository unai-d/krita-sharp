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

		KritaProject kra;

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
				kra = new KritaProject(args[1]);
				DescribeProject(kra);
				break;

			case "x":
			case "extract":
				kra = new KritaProject(args[1]);
				var layer = kra.GetLayerByUuid(Guid.Parse(args[2]));
				var rlayer = kra.GetRasterLayer(layer);
				Stream outputStream = args.Length > 3 ? File.OpenWrite(args[3]) : Console.OpenStandardOutput();
				outputStream.Write(rlayer.GetAsBmp());
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
}
