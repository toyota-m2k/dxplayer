using dxplayer.data.main;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dxplayer.settings
{
    public interface IListFilter {
        IEnumerable<PlayItem> Filter(IEnumerable<PlayItem> list);
    }
    public class ListFilter : PropertyChangeNotifier, IListFilter {
        public event Action FilterUpdated;

        private bool mEnabled = true;
        public bool Enabled {
            get => mEnabled;
            set { setProp(callerName(), ref mEnabled, value); FilterUpdated?.Invoke(); }
        }

        private bool mExcellent = true;
        public bool Excellent {
            get => mExcellent;
            set { setProp(callerName(), ref mExcellent, value); FilterUpdated?.Invoke(); }
        }
        private bool mGood = true;
        public bool Good {
            get => mGood;
            set { setProp(callerName(), ref mGood, value); FilterUpdated?.Invoke(); }
        }
        private bool mNormal = true;
        public bool Normal {
            get => mNormal;
            set { setProp(callerName(), ref mNormal, value); FilterUpdated?.Invoke(); }
        }
        private bool mBad = false;
        public bool Bad {
            get => mBad;
            set { setProp(callerName(), ref mBad, value); FilterUpdated?.Invoke(); }
        }
        private bool mDreadful = false;
        public bool Dreadful {
            get => mDreadful;
            set { setProp(callerName(), ref mDreadful, value); FilterUpdated?.Invoke(); }
        }

        private int mPlayCount = 0;
        public int PlayCount {
            get => mPlayCount;
            set { setProp(callerName(), ref mPlayCount, value); FilterUpdated?.Invoke(); }
        }
        public enum Comparison
        {
            NONE,
            EQ,     // ==
            LE,      // <=
            GE,      // >=
        }
        private Comparison mPlayCountCP = Comparison.NONE;
        public Comparison PlayCountCP {
            get => mPlayCountCP;
            set { setProp(callerName(), ref mPlayCountCP, value); FilterUpdated?.Invoke(); }
        }

        public enum BoolFilter {
            NONE,
            TRUE,
            FALSE,
        }

        private BoolFilter mChecked = BoolFilter.NONE;
        public BoolFilter Checked {
            get => mChecked;
            set { setProp(callerName(), ref mChecked, value); FilterUpdated?.Invoke(); }
        }


        public IEnumerable<PlayItem> Filter(IEnumerable<PlayItem> list) {
            list = list.Where(c => c.Flag == 0);
            if (!Enabled) return list;
            if(!Excellent) {
                list = list.Where(c => c.Rating != Rating.EXCELLENT);
            }
            if (!Good) {
                list = list.Where(c => c.Rating != Rating.GOOD);
            }
            if (!Normal) {
                list = list.Where(c => c.Rating != Rating.NORMAL);
            }
            if (!Bad) {
                list = list.Where(c => c.Rating != Rating.BAD);
            }
            if (!Dreadful) {
                list = list.Where(c => c.Rating != Rating.DREADFUL);
            }

            switch(PlayCountCP) {
                case Comparison.EQ:
                    list = list.Where(c => c.PlayCount == PlayCount); break;
                case Comparison.LE:
                    list = list.Where(c => c.PlayCount <= PlayCount); break;
                case Comparison.GE:
                    list = list.Where(c => c.PlayCount >= PlayCount); break;
                default:
                    break;
            }

            switch(Checked) {
                case BoolFilter.TRUE:
                    list = list.Where(c => c.Checked); break;
                case BoolFilter.FALSE:
                    list = list.Where(c => !c.Checked); break;
                default:
                    break;
            }

            return list;
        }
    }
}
