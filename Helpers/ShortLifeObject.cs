using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarryAnyone.Helpers
{
    internal class ShortLifeObject
    {

        protected Object? _o;
        protected DateTime _born;
        protected int _delayInMicroseconde;

        public ShortLifeObject(int delayInMicroseconde)
        {
            _o = null;
            _born = DateTime.Now;
            _delayInMicroseconde = delayInMicroseconde;
        }

        public bool Swap(Object pO)
        {
            DateTime now = DateTime.Now;
            if (_o != pO
                || (_o == pO && now.Subtract(_born).TotalMilliseconds > _delayInMicroseconde)) // _born.Subtract(now).TotalMilliseconds > 100))
            {
                _o = pO;
                _born = now;
                return true;
            }
            return false;
        }

        public virtual void Done()
        {
            _o = null;
        }
    }

    internal class ShortLifeBiObject : ShortLifeObject
    {
        protected Object? _o2;

        public ShortLifeBiObject(int delayInMicroseconde) : base(delayInMicroseconde)
        {
            _o2 = null;
        }

        public ShortLifeBiObject(int delayInMicroseconde, object? o, object? o2) : base(delayInMicroseconde)
        {
            _o = o;
            _o2 = o2;
        }

        public bool Swap(Object pO, Object pO2)
        {
            DateTime now = DateTime.Now;
            if (_o != pO
                || _o2 != pO2
                || (_o == pO && _o2 == pO2 && now.Subtract(_born).TotalMilliseconds > _delayInMicroseconde)) // _born.Subtract(now).TotalMilliseconds > 100))
            {
                _o = pO;
                _born = now;
                return true;
            }
            return false;
        }

        public bool Resolve(Object pO, Object pO2)
        {
            DateTime now = DateTime.Now;
            bool delaiOk = now.Subtract(_born).TotalMilliseconds <= _delayInMicroseconde;
            if (_o == pO && _o2 == pO2 && delaiOk)
            {
                _born = now;
                return true;
            }

            if (!delaiOk)
                Done();

            return false;
        }

        public bool IsEmpty()
        {
            return _o == null && _o2 == null;
        }

        public override void Done()
        {
            _o2 = null;
            base.Done();
        }
    }

    internal class ShortLifeBiObjects
    {
        public List<ShortLifeBiObject> _listO;
        protected int _delayInMicroseconde;

        public ShortLifeBiObjects(int delayInMicroseconde)
        {
            _delayInMicroseconde = delayInMicroseconde;
            _listO = new List<ShortLifeBiObject>();
        }

        // Return false if not resolved
        public bool Swap(Object pO, Object pO2)
        {
            bool resolve = false;
            for (int i = 0; i < _listO.Count; i++)
            {
                resolve = _listO[i].Resolve(pO, pO2);
                if (resolve)
                    return false;

                if (!resolve && _listO[i].IsEmpty())
                {
                    _listO.RemoveAt(i);
                    i--;
                }
            }

            _listO.Add(new ShortLifeBiObject(_delayInMicroseconde, pO, pO2));
            return true;
        }

        public void Done()
        {
            foreach (ShortLifeBiObject o in _listO)
                o.Done();
            _listO.Clear();
        }
    }
}