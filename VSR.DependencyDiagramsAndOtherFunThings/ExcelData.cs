using System;
using System.Collections.Generic;
using System.Data;
using DapperLogic;
using ExcelDataReader;
using ItemRandomizer;

namespace ExcelLogicBridge {
	class ExcelData {
		//private DataSet _dataSet;

		public List<LocationRow> Locations = new List<LocationRow>();
		public List<MacroRow> Macros = new List<MacroRow>();

		public ExcelData(IExcelDataReader reader) {
			var _dataSet = reader.AsDataSet();

			var locations = _dataSet.Tables[0].Rows;
			for (int i = 1; i < locations.Count; i++) {
				Locations.Add(new LocationRow(i, locations[i]));
			}

			var macros = _dataSet.Tables[1].Rows;
			for (int i = 1; i < macros.Count; i++) {
				Macros.Add(new MacroRow(i, macros[i]));
			}
		}

		public class LocationRow : ExcelRow {
			public LocationRow(int rowNum, DataRow data) : base(data, 0, rowNum) { }

			public Location RandoLocation { get; set; }

			public byte Number => _byte(0);
			public string Area => _string(1);
			public string Scene => _string(2);
			public int MapX => _int(3);
			public int MapY => _int(4);
			public string GameObject => _string(5);
			public string IDString => _string(6);
			public string LogicString {
				get => _string(7);
				set => _makeDiff(7, value);
			}

			public string AutoMagic {
				get => _string(8);
				set => _makeDiff(8, value);
			}

			public DateTime LastImported {
				get => _date(9);
				set => _makeDiff(9, value.ToString());
			}

			public string FunStuff {
				get => _string(12);
				set => _makeDiff(12, value);
			}

			public bool NeedsSimplify = false;
			private List<string> _newLogics = null;

			public void AddLogic(string logic) {
				(_newLogics ??= new List<string> { LogicString }).Add(logic);
				NeedsSimplify = true;
			}

			public void SimplifyLogic() {
				AutoMagic = LogicString;
				LogicString = ExcelCompare.SimplifyFromList(_newLogics);
				LastImported = DateTime.Now;
				NeedsSimplify = false;
			}
		}

		public class MacroRow : ExcelRow {
			public MacroRow(int rowNum, DataRow data) : base(data, 1, rowNum) { }

			public Macro RandoMacro { get; set; }

			public string Name => _string(0);
			public string LogicString => _string(1);
		}

		public class ExcelRow {
			public class Diff {
				public int SheetNum { get; } = -1;
				public int RowNum { get; } = -1;
				public int ColNum { get; } = -1;
				public string Value { get; } = "";

				public Diff(int sheetNum, int rowNum, int colNum, string value) {
					this.SheetNum = sheetNum + 1;
					this.RowNum = rowNum + 1;
					this.ColNum = colNum + 1;
					this.Value = value;
				}
			}

			public bool HasDiffs => _diffs.Count > 0;
			public Diff[] Diffs => _diffs.ToArray();

			protected DataRow _data;
			protected int _sheetNum;
			protected int _rowNum;
			private List<Diff> _diffs = new List<Diff>();

			public ExcelRow(DataRow data, int sheetNum, int rowNum) {
				this._data = data;
				this._sheetNum = sheetNum;
				this._rowNum = rowNum;
			}

			protected string _string(int index) {
				return _data.ItemArray[index].ToString();
			}

			protected byte _byte(int index) {
				if (byte.TryParse(_string(index), out byte result)) return result;
				return default(byte);
			}
			protected int _int(int index) {
				if (int.TryParse(_string(index), out int result)) return result;
				return default(int);
			}

			protected DateTime _date(int index) {
				if (DateTime.TryParse(_string(index), out DateTime result)) return result;
				return default(DateTime);
			}



			protected void _makeDiff(int colNum, string value) {
				_diffs.Add(new Diff(_sheetNum, _rowNum, colNum, value));
			}
		}
	}
}
