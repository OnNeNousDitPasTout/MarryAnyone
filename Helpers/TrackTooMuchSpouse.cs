using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MarryAnyone.Helpers
{
#if TRACKTOMUCHSPOUSE

    internal class TrackTooMuchSpouse : IDisposable
    {

        static TrackTooMuchSpouse? _instance;


        List<Hero>? _spouses;
        int _nbSpouse = -1;

        private void update()
        {
            if (_spouses != null)
            {
                _nbSpouse = _spouses.Count;
                Helper.Print(String.Format("TrackTooMuchSpouse update spouse : {0}", String.Join(", ", _spouses.Select<Hero, String>(x => x.Name.ToString()).ToList())), Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
            }
            else {
                _nbSpouse = 0;
                Helper.Print("TrackTooMuchSpouse update spouse to EMPTY", Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
            }
        }

        // Return true if number change
        public bool Verify(List<Hero> spouses, int diff = 0, String prefix = null)
        {
            if (_nbSpouse == -1) // not Initialised
                return false;

            int nbSpouse = 0;
            if (spouses != null)
                nbSpouse = spouses.Count;

            int nbAdd = 0;
            int nbRemove = 0;

            List<Hero> added = null;
            if (spouses != null) {
                added = spouses.ToList();
                if (_nbSpouse > 0)
                {
                    added.RemoveAll(x => _spouses.IndexOf(x) >= 0);
                }
                nbAdd = added.Count;
            }
            List<Hero> removed = null;
            if (_nbSpouse > 0)
            {
                removed = _spouses.ToList();
                if (spouses != null)
                {
                    removed.RemoveAll(x => spouses.IndexOf(x) >= 0);
                }
                nbRemove = removed.Count();
            }

            bool enErreur = false;
            if (diff >= 0 && nbAdd != diff)
                enErreur = true;
            if (diff <= 0 && nbRemove != -diff)
                enErreur = true;

            if (nbAdd != 0 || nbRemove != 0)
            {

                String aff = (prefix != null ? prefix : "");
                if (enErreur)
                    aff = String.Format("Diff unexpected {0} against {1}", (nbAdd - nbRemove), diff);
                if (nbAdd > 0) {
                    if (!String.IsNullOrWhiteSpace(aff))
                        aff += "\r\n";
                    aff += String.Format("You just have added {0} spouse {1}", nbAdd, String.Join(", ", added.Select<Hero, String>(x => x.Name.ToString()).ToList()));
                }
                if (nbRemove > 0) {
                    if (!String.IsNullOrWhiteSpace(aff))
                        aff += "\r\n";
                    aff += String.Format("You just have removed {0} spouse {1}", nbRemove, String.Join(", ", removed.Select<Hero, String>(x => x.Name.ToString()).ToList()));
                }

                Helper.Print(aff, Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
                return true;
            }
            return false;
        }

        public void Validate(List<Hero> spouses)
        {
            _spouses = spouses;
            update();
        }

        public void  Initialise(List<Hero> spouses)
        {
            if (_nbSpouse == -1)
            {
                _spouses = spouses;
                update();
                Helper.Print(String.Format("TrackTooMuchSpouse initialize {0} spouses", _nbSpouse), Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
            }
            else
                Helper.Print("TrackTooMuchSpouse allready initialized", Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
        }

        #region vie de l'objet
        public static TrackTooMuchSpouse Instance()
        {
            if (_instance == null)
                _instance = new TrackTooMuchSpouse();

            return _instance;
        }

        public TrackTooMuchSpouse()
        {
        }

        public void Dispose()
        {
            if (_instance == this)
                _instance = null;

            _spouses = null;
            _nbSpouse = -1;
        }
        #endregion

    }
#endif
}
