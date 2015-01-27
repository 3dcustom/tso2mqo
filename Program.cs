using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Tso2MqoGui
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length != 0)
            {
                // バッチで処理する
                try
                {
                    string tso_file = null;
                    string mqo_file = null;
                    string tsoref_file = null;
                    string out_path = null;

                    foreach (string arg in args)
                    {
                        string opt = arg.ToLower();

                        if (opt.StartsWith("-tso:"))
                            tso_file = opt.Substring(5).Trim('\r', '\n');
                        else if (opt.StartsWith("-mqo:"))
                            mqo_file = opt.Substring(5).Trim('\r', '\n');
                        else if (opt.StartsWith("-ref:"))
                            tsoref_file = opt.Substring(5).Trim('\r', '\n');
                        else if (opt.StartsWith("-out:"))
                            out_path = opt.Substring(5).Trim('\r', '\n');
                        else
                            throw new ArgumentException("Invalid option: " + arg);
                    }

                    if (tso_file == null)
                        throw new ArgumentException("-tso:ファイル名 の形式で出力Tsoファイル名を指定してください");

                    if (out_path != null)
                    {
                        MqoGenerator gen = new MqoGenerator();
                        gen.Generate(tso_file, out_path, false);
                    }
                    else
                    {
                        if (mqo_file == null)
                            throw new ArgumentException("-mqo:ファイル名 の形式で入力Mqoファイル名を指定してください");

                        TSOGeneratorConfig config = new TSOGeneratorConfig();
                        config.cui = true;
                        config.ShowMaterials = false;
                        if (tsoref_file == null)
                        {
                            TSOGeneratorMqxBone gen = new TSOGeneratorMqxBone(config);
                            gen.Generate(mqo_file, tsoref_file, tso_file);
                        }
                        else
                        {
                            TSOGeneratorRefBone gen = new TSOGeneratorRefBone(config);
                            gen.Generate(mqo_file, tsoref_file, tso_file);
                        }
                    }
                }
                catch (ArgumentException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    System.Console.Out.WriteLine(e.Message);
                    System.Console.Out.Flush();
                    return 1;
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message);
                    System.Console.Out.WriteLine(exception.Message);
                    System.Console.Out.Flush();
                    return 1;
                }

                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            return 0;
        }
    }
}
