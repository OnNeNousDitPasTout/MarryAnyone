using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarryAnyone.Helpers
{
    class ShortLifeObject
    {

        private Object? _o;
        private DateTime _born;

        public ShortLifeObject()
        {
            _o = null;
            _born = DateTime.Now;
        }

        public bool Swap(Object pO)
        {
            DateTime now = DateTime.Now;
            if (_o == null 
                || _o != pO 
                || (_o == pO && _born.Subtract(now).TotalMilliseconds > 100))
            {
                _o = pO;
                _born = now;
                return true;
            }
            return false;
        }

        public void Done()
        {
            _o = null;
        }
    }
}
