using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSR.DependencyDiagramsAndOtherFunThings {
	//type=OutOfLogic&version=1.1.0&user=Felissan&seed=1413068883&location=orb1%3aOrb1&options=puzzles&decryptors=CHARGE_SHOT%2cWALL_RUN%2cENERGY_CLAW%2cSTRIP_SUIT%2cSPEED_BOOST&time=9%2F7%2F2022+5%3A53%3A40+AM 
	public class LanimalsData {
		public class LocationRow : LanimalsRow {

			public LocationRow(NameValueCollection nvc) : base(nvc) {
				IDStr = _GetOrThrow(nvc, "idstr");
				Location = _GetOrThrow(nvc, "location");
				Options = (nvc["options"] ?? "").Split(',').ToList();
				Decryptors = _GetOrThrow(nvc, "decryptors");
			}
			public string IDStr { get; private set; }
			public string Location { get; private set; }
			public List<string> Options { get; private set; }
			public string Decryptors { get; private set; }

			private string _logic = null;
			public string Logic => _logic ??= _MakeLogic();

			private string _scene = null;
			public string Scene => _scene ??= Location.Split(':')[0];

			private string _gameObject = null;
			public string GameObject => _gameObject ??= Location.Split(':')[1];

			private string _MakeLogic() {
				string retVal = $"{_GetPuzzleOption(Options.Contains("puzzles"))}";
				if (Decryptors != "") {
					retVal += $" & {Decryptors.Replace(",", " & ")}";
				}

				return retVal;
			}

			private string _GetPuzzleOption(bool enabled = false) {
				return enabled ? "opt_randomized_puzzles" : "!opt_randomized_puzzles";
			}
		}

		public class LanimalsRow {
			public LanimalsRow(NameValueCollection nvc) {
				Type = _GetOrThrow(nvc, "type");
				Version = _GetOrThrow(nvc, "version");
				User = _GetOrThrow(nvc, "user");
				if (DateTime.TryParse(nvc["time"], out DateTime dt)) {
					DateTime = dt;
				}
			}

			public string Type { get; private set; }
			public string Version { get; private set; }
			public string User { get; private set; }
			public DateTime DateTime { get; private set; } = DateTime.MinValue;
		}

		private static string _GetOrThrow(NameValueCollection nvc, string key) {
			return nvc[key] ?? throw new ArgumentNullException(key, "Inappropriate null!");
		}

		private enum NVCType {
			String,
			DateTime
		}
	}
}
