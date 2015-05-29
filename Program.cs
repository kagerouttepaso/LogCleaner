using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogCleaner {
	/// <summary>
	/// 文字列のコンテナ
	/// </summary>
	class StringContainer {
		/// <summary>
		/// 行番号
		/// </summary>
		public int Number { set; get; }
		/// <summary>
		/// 文字列
		/// </summary>
		public string String { set; get; }
	}

	class Program {
		/// <summary>
		/// ファイルを最後まで一行ずつ読み込む
		/// </summary>
		/// <param name="fs">ファイルストリーム</param>
		/// <returns>1行分の情報</returns>
		static IEnumerable<StringContainer> YieldReadToEnd(StreamReader fs) {
			int i = 0;
			while(!fs.EndOfStream) {
				i++;
				yield return new StringContainer() {
					Number = i,
					String = fs.ReadLine()
				};
			}
		}

		/// <summary>
		/// メイン関数
		/// </summary>
		/// <param name="args">編集するファイル郡</param>
		static void Main(string[] args) {
			var regexName = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName + @"\" + "regex.conf";
			
			//除外リストを取得
			if(!File.Exists(regexName)) { return; }
			var regexs = File.ReadAllText(regexName)
				.Replace("\r\n", "\n")
				.Split('\n')
				.Where(s => s != "")
				.Where(s => !Regex.IsMatch(s,"^###"))
				.ToList();


			//再帰処理
			args.ToList().ForEach(fileName => {
				if(!File.Exists(fileName)) { return; }
				Console.WriteLine(fileName);
				var time = DateTime.Now;

				//ファイル変換
				Console.WriteLine("File reading");
				var consoleLock = new object();
				var safeBag = new ConcurrentBag<StringContainer>();
				Func<string, bool> action = s => regexs.Any(x => Regex.IsMatch(s, x));

				using(var fs = new StreamReader(fileName)) {
					Parallel.ForEach(YieldReadToEnd(fs), x => {
						if((x.Number % 100000) == 0x0000) {
							lock(consoleLock)
								Console.WriteLine("line {0}", x.Number);
						}
						if(action(x.String))
							return;
						safeBag.Add(x);
					});
				}
				Console.WriteLine(DateTime.Now - time);

				//ファイル出力
				Console.WriteLine("File writing");
				var fInfo = new FileInfo(fileName);
				var str = string.Join("\n", safeBag
					.OrderBy(x => x.Number)
					.Select(x => x.String));
				using(var fs = new StreamWriter(fInfo.DirectoryName + @"\" + "整形後" + fInfo.Name)) {
					fs.Write(str);
				}

				Console.WriteLine(DateTime.Now - time);
				Console.WriteLine();
			});

			Console.Read();
		}
	}
}
