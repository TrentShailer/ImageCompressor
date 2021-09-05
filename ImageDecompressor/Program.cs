using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImageDecompressor
{
	class Instruction
	{
		public Color color
		{
			get;
			private set;
		}

		public Int32 pixels
		{
			get;
			private set;
		}

		public Instruction(Color _color, Int32 _pixels)
		{
			color = _color;
			pixels = _pixels;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("No File Provided");
				Thread.Sleep(5000);
				return;
			}

			string pathToTxt = args[0];

			if (!Regex.IsMatch(pathToTxt, @".*(\.txt)$"))
			{
				Console.WriteLine("Invalid File Type");
				Thread.Sleep(5000);
				return;
			}

			String contents = File.ReadAllText(pathToTxt);

			String[] parts = contents.Split("|");

			String[] sizeStrings = parts[0].Split(",");
			Int32 width = Int32.Parse(sizeStrings[0]);
			Int32 height = Int32.Parse(sizeStrings[1]);
			Bitmap image = new Bitmap(width, height);

			String huffmanString = parts[1];
			Dictionary<char, Color> huffmanTree = new Dictionary<char, Color>();

			char key = 'a';
			String value = "";
			int keyPairIndex = 0;
			for (int i = 0; i < huffmanString.Length; i++)
			{
				char c = huffmanString[i];

				if (keyPairIndex == 0) key = c;
				else value += c;
				if (keyPairIndex == 7)
				{
					huffmanTree.Add(key, ColorTranslator.FromHtml(value));
					value = "";
					keyPairIndex = 0;
					continue;
				}
				keyPairIndex++;
			}


			String rle = parts[2];
			bool hex = false;
			String curHex = "";
			String curPixels = "";
			Int32 hexIndex = 0;
			Color currentColor = new Color();

			List<Instruction> instructions = new List<Instruction>();

			if (rle[0] == '#')
			{
				hex = true;
				hexIndex = 0;
				curHex += rle[0];
			}
			else
			{
				currentColor = huffmanTree[rle[0]];
			}

			for (int i = 1; i < rle.Length; i++)
			{
				char c = rle[i];

				if (hex)
				{
					hexIndex++;
					curHex += c;
					if (hexIndex == 5)
					{
						hex = false;
						currentColor = ColorTranslator.FromHtml(curHex);
						curHex = "";
					}
				}
				else
				{
					if (Regex.IsMatch(c.ToString(), "\\d"))
					{
						curPixels += c;
					}
					else
					{
						Console.WriteLine(curPixels);
						instructions.Add(new Instruction(currentColor, Int32.Parse(curPixels)));
						curPixels = "";
						if (c == '#')
						{
							hex = true;
							hexIndex = 0;
							curHex += c;
						}
						else
						{
							currentColor = huffmanTree[c];
						}
					}

				}
			}

			instructions.Add(new Instruction(currentColor, Int32.Parse(curPixels)));


			Instruction currentInstruction = instructions[0];
			int index = 0;
			int pixels = 0;
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					pixels++;
					image.SetPixel(x, y, currentInstruction.color);

					if (currentInstruction.pixels == pixels)
					{

						pixels = 0;
						index++;
						if (index != instructions.Count)
							currentInstruction = instructions[index];
					}


				}
			}
			image.Save(Regex.Replace(pathToTxt, @"\.txt$", "Decompressed.png"), System.Drawing.Imaging.ImageFormat.Png);

			Console.WriteLine("Finished");
			Thread.Sleep(30000);
		}

	}
}
