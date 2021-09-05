using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RunLengthEncoding
{
	class Program
	{

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("No Image Provided");
				Thread.Sleep(5000);
				return;
			}

			string pathToImg = args[0];

			if (!Regex.IsMatch(pathToImg, @".*(\.png|\.bmp|\.jpg|\.jpeg)$"))
			{
				Console.WriteLine("Invalid File Type");
				Thread.Sleep(5000);
				return;
			}


			Bitmap img = new Bitmap(Image.FromFile(pathToImg));


			Dictionary<String, String> settings = GetSettings();

			if (settings["type"] == "2" && (img.Width != 480 || img.Height != 272))
			{
				Console.WriteLine("For Vex output, image must be 480x272");
				Thread.Sleep(5000);
				return;
			}

			Console.WriteLine("Image Loaded!");
			Console.WriteLine("Width: " + img.Width);
			Console.WriteLine("Height: " + img.Height);
			Console.WriteLine("Total Pixels: " + img.Width * img.Height);

			if (settings["simplify"] == "y")
			{
				Thread.Sleep(500);
				Console.WriteLine("Simplifying");
				Thread.Sleep(500);
				var rt = System.Diagnostics.Stopwatch.StartNew();
				img = Simplify(img);
				rt.Stop();
				Console.WriteLine("Took: " + rt.ElapsedMilliseconds + "ms");
			}


			Thread.Sleep(500);
			Console.WriteLine("Generating Huffman Tree");
			Thread.Sleep(500);
			var watch = System.Diagnostics.Stopwatch.StartNew();
			Dictionary<Color, String> HuffmanTree = GenerateHuffmanTree(img);
			watch.Stop();
			Console.WriteLine("Took: " + watch.ElapsedMilliseconds + "ms");

			Thread.Sleep(500);
			Console.WriteLine("Compressing");
			Thread.Sleep(500);
			watch = System.Diagnostics.Stopwatch.StartNew();
			String rle = GenerateRLE(img, HuffmanTree, settings);
			watch.Stop();
			Console.WriteLine("Took: " + watch.ElapsedMilliseconds + "ms");
			Thread.Sleep(500);
			if (settings["type"] == "1")
			{
				File.WriteAllText(Regex.Replace(pathToImg, @"(\.png|\.bmp|\.jpg|\.jpeg)$", ".txt"), rle);
			}
			else if (settings["type"] == "2")
			{
				File.WriteAllText(Regex.Replace(pathToImg, @"(\.png|\.bmp|\.jpg|\.jpeg)$", ".h"), $"std::string rle = \"{rle}\"");
			}

			Console.WriteLine("Finished!");
			Console.WriteLine("Estimated File Size: " + ToReadableSize(rle.Length));

			Thread.Sleep(30000);
		}

		static String ToReadableSize(int bytes)
		{
			String unit = "B";
			float divider = 1f;

			if (bytes >= 1000000000)
			{
				unit = "GB";
				divider = 1000000000;
			}
			else if (bytes >= 1000000)
			{
				unit = "MB";
				divider = 1000000;
			}
			else if (bytes >= 1000)
			{
				unit = "KB";
				divider = 1000;
			}

			String readable = (bytes / divider).ToString("n2");
			return $"{readable} {unit}";
		}

		static String GenerateRLE(Bitmap img, Dictionary<Color, String> huffmanTree, Dictionary<String, String> settings)
		{
			String rle = $"{img.Width},{img.Height}|";

			KeyValuePair<Color, String>[] huffmanArray = huffmanTree.ToArray();

			for (int i = 0; i < huffmanArray.Length; i++)
			{
				KeyValuePair<Color, String> item = huffmanArray[i];
				rle += item.Value;
				rle += ToHex(item.Key);
			}
			rle += "|";

			Color prevColor = new Color();
			Color curColor = new Color();
			int count = 0;
			int prevCount = 0;

			for (int y = 0; y < img.Height; y++)
			{
				for (int x = 0; x < img.Width; x++)
				{
					Color color = img.GetPixel(x, y);
					if (color == curColor) count++;
					else
					{
						if (count < UInt16.Parse(settings["pixels"]))
						{
							prevCount += count;
							count = 1;
							curColor = color;
						}
						else
						{
							if (prevCount > 0)
							{
								if (huffmanTree.ContainsKey(prevColor))
								{
									rle += huffmanTree[prevColor];
								}
								else
								{
									rle += ToHex(prevColor);
								}
								rle += prevCount;
							}
							prevColor = curColor;
							prevCount = count;
							curColor = color;
							count = 1;
						}
					}
				}
			}
			if (huffmanTree.ContainsKey(curColor))
			{
				rle += huffmanTree[curColor];
			}
			else
			{
				rle += ToHex(curColor);
			}
			rle += count;

			return rle;
		}

		static Dictionary<Color, String> GenerateHuffmanTree(Bitmap img)
		{
			Dictionary<Color, int> counter = new Dictionary<Color, int>();
			for (int y = 0; y < img.Height; y++)
			{
				for (int x = 0; x < img.Width; x++)
				{
					Color color = img.GetPixel(x, y);
					if (counter.ContainsKey(color))
					{
						counter[color] = counter[color] + 1;
					}
					else
					{
						counter.Add(color, 1);
					}
				}
			}

			IOrderedEnumerable<KeyValuePair<Color, int>> sortedTree = from pair in counter
																	  orderby pair.Value descending
																	  select pair;
			IEnumerable<KeyValuePair<Color, int>> huffmanPairs = sortedTree.Take(52);

			Dictionary<Color, String> huffmanTree = new Dictionary<Color, String>();

			String[] huffmanKeys = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

			KeyValuePair<Color, int>[] huffmanPairsArray = huffmanPairs.ToArray();

			for (int i = 0; i < huffmanPairsArray.Count(); i++)
			{
				KeyValuePair<Color, int> pair = huffmanPairsArray[i];
				huffmanTree.Add(pair.Key, huffmanKeys[i]);
			}

			return huffmanTree;
		}

		static byte SimplifyColor(byte value)
		{
			Dictionary<char, char> rounding = new Dictionary<char, char>{
				{'0', '0'},
				{'1', '3'},
				{'2', '3'},
				{'3', '3'},
				{'4', '3'},
				{'5', '6'},
				{'6', '6'},
				{'7', '6'},
				{'8', '9'},
				{'9', '9'}
			};
			String str = value.ToString();
			str = str.PadLeft(3, '0');
			char[] ch = str.ToCharArray();
			ch[1] = rounding[ch[1]];
			ch[2] = '0';
			str = new String(ch);
			int i = Int32.Parse(str);
			i = Math.Clamp(i, 0, 255);
			return (byte)i;
		}

		static Bitmap Simplify(Bitmap img)
		{
			for (int y = 0; y < img.Height; y++)
			{
				for (int x = 0; x < img.Width; x++)
				{
					Color color = img.GetPixel(x, y);
					color = Color.FromArgb(255, SimplifyColor(color.R), SimplifyColor(color.G), SimplifyColor(color.B));
					img.SetPixel(x, y, color);
				}
			}
			return img;
		}

		static String ToHex(Color c)
		{
			return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
		}


		static Dictionary<String, String> GetSettings()
		{
			Dictionary<String, String> settings = new Dictionary<String, String>();

			while (true)
			{
				Console.Write("Simplify Colors (y/n): ");
				String response = Console.ReadLine();
				if (response == "y" || response == "n")
				{
					settings.Add("simplify", response);
					break;
				}
			}

			while (true)
			{
				Console.WriteLine("Output Type?");
				Console.WriteLine("(1) Text File");
				Console.WriteLine("(2) Vex File");
				String response = Console.ReadLine();
				if (response == "1" || response == "2")
				{
					settings.Add("type", response);
					break;
				}
			}

			while (true)
			{
				Console.WriteLine("Ignore runs less than x pixels? ");
				String response = Console.ReadLine();
				if (UInt16.TryParse(response, out UInt16 number))
				{
					settings.Add("pixels", response);
					break;
				}
			}

			return settings;
		}


	}
}
