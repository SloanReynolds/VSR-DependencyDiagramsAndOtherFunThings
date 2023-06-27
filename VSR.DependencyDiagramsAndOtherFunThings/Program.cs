using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using ItemRandomizer;

namespace ExcelLogicBridge {
	class Program {
		const string FORCE_ADD = "(opt_randomized_puzzles & EvErYtHiNg)";
		const bool FORCE_ADD_OR = true;

		static void Main(string[] args) {
			while (true) {
				Console.WriteLine("WHATCHU WANT?");
				Console.WriteLine("1) Export xlsx => json");
				Console.WriteLine("2) Import lanimals => xlsx");
				Console.WriteLine("6) Full location");
				Console.WriteLine("7) Full Full Import (FunStuff Column 12)");
				Console.WriteLine($"9) Force add `{FORCE_ADD} {(FORCE_ADD_OR ? "|" : "&")} <logic>` => xlsx");
				Console.WriteLine("x) = Exit");

				ConsoleKeyInfo cki = Console.ReadKey();
				Console.WriteLine();

				if (cki.Key == ConsoleKey.D1) {
					//Start Excel Compare
					Console.WriteLine("Exporting: xlsx => json");

					ExcelCompare ec = new ExcelCompare();
					ec.ParseExcelSheet();
					ec.ExportToProjectJSON();

					Console.WriteLine("Export Finished!");
					Console.WriteLine();

					continue;
				}

				if (cki.Key == ConsoleKey.D2) {
					//Start Excel Compare
					Console.WriteLine("Importing: lanimals => xlsx");

					//Get OG Data
					ExcelCompare ec = new ExcelCompare();
					ec.ParseExcelSheet();

					//Download New LogicStrings and Modify
					ec.DownloadFromLanimals();

					//Write the bitch down
					ec.ImportToExcel();

					Console.WriteLine("Import Finished!");
					Console.WriteLine();

					continue;
				}

				if (cki.Key == ConsoleKey.D6) {
					//Enter and Validate Location
					Console.WriteLine("Which Location? Enter by idstr [eg. 'Card_Rupo']");
					string input = Console.ReadLine();
					var loc = Locations.Initial.FirstOrDefault(l => l.IDStr == input);
					if (loc == null) {
						Console.WriteLine($"Location '{loc}' not found. Did you make a boo boo?");
						Console.ReadKey();
						continue;
					}

					var allLogics = FullThing(input);

					//Reduce
					string expanded = ExcelCompare.BuildNewLogicStr(allLogics);
					string simplified = ExcelCompare.SimplifyFromString(expanded);

					Console.WriteLine("Fully Expanded:");
					Console.WriteLine(expanded);
					Console.WriteLine();

					Console.WriteLine("Simplified:");
					Console.WriteLine(simplified);
					Console.WriteLine();

					continue;
				}

				if (cki.Key == ConsoleKey.D7) {
					//Start Excel Compare
					Console.WriteLine("Making Full Full Report to FunStuff LUL");

					//Get OG Data
					ExcelCompare ec = new ExcelCompare();
					ec.ParseExcelSheet();

					//Download New LogicStrings and Modify
					foreach (var locRow in ec.Locations) {
						locRow.FunStuff = ExcelCompare.SimplifyFromList(FullThing(locRow.IDString));
						Console.WriteLine($"-- {locRow.IDString} Done");
					}

					//Write the bitch down
					ec.ImportToExcel();

					Console.WriteLine("FullFull Finished!");
					Console.WriteLine();

					continue;
				}

				if (cki.Key == ConsoleKey.D9) {
					ExcelCompare ec = new ExcelCompare();
					ec.ParseExcelSheet();
					ec.ForceAddSymbol(FORCE_ADD, FORCE_ADD_OR);
					ec.ImportToExcel();

					Console.WriteLine("Import Finished!");
					Console.WriteLine();

					continue;
				}

				if (cki.Key == ConsoleKey.X) {
					Console.WriteLine();
					break;
				}
				Console.WriteLine();
			}
			Console.WriteLine("Bye bye!");
			Console.ReadKey();
		}

		static List<string> FullThing(string idStr) {
			//Start with EvErYtHiNg
			List<string> allLogics = new List<string> {
						"EvErYtHiNg & opt_randomized_puzzles",
						"EvErYtHiNg & !opt_randomized_puzzles"
					};

			//Pull all data from Lanimals
			foreach (var item in ExcelCompare.AllLanimalsRowsForIDStr(idStr)) {
				allLogics.Add(item.Logic);
			}

			return allLogics;
		}
	}
}
