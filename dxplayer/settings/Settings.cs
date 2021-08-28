using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace dxplayer.settings
{
    public class Settings {
        public WinPlacement Placement { get; set; } = new WinPlacement();
        public WinPlacement PlayerPlacement { get; set; } = new WinPlacement();
        public string FilePath { get; set; } = "default.dpd";
        public ListFilter ListFilter { get; set; } = new ListFilter();
        public SortInfo SortInfo { get; set; } = new SortInfo();
        public List<string> MRU { get; set; } = new List<string>();
        public string DxxDBPath { get; set; } = "";
        public string LastPlayingPath { get; set; } = "";
        public ulong LastPlayingPos { get; set; } = 0;
        public bool UseServer { get; set; } = false;
        public bool PlayCountFromServer { get; set; } = false;
        public int ServerPort { get; set; } = 5000;

        private static readonly string SETTINGS_FILE = "dxplayer.settings";

        private static Settings sInstance = null;
        public static Settings Instance {
            get {
                if (sInstance == null) {
                    sInstance = Deserialize();
                }
                return sInstance;
            }
        }

#if DEBUG
        public static bool IsDebug = true;
#else
        public static bool IsDebug = false;
#endif

        public void Serialize() {
            System.IO.StreamWriter sw = null;
            try {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                //書き込むファイルを開く（UTF-8 BOM無し）
                sw = new System.IO.StreamWriter(SETTINGS_FILE, false, new System.Text.UTF8Encoding(false));
                //シリアル化し、XMLファイルに保存する
                serializer.Serialize(sw, this);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
            finally {
                //ファイルを閉じる
                if (null != sw) {
                    sw.Close();
                }
            }
        }

        public static Settings Deserialize() {
            System.IO.StreamReader sr = null;
            Object obj = null;

            try {
                //XmlSerializerオブジェクトを作成
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                //読み込むファイルを開く
                sr = new System.IO.StreamReader(SETTINGS_FILE, new System.Text.UTF8Encoding(false));

                //XMLファイルから読み込み、逆シリアル化する
                obj = serializer.Deserialize(sr);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                obj = new Settings();
            }
            finally {
                if (null != sr) {
                    //ファイルを閉じる
                    sr.Close();
                }
            }
            return (Settings)obj;
        }

    }
}
