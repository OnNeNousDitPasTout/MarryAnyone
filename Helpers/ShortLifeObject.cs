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
        private int _delayInMicroseconde;

        public ShortLifeObject(int delayInMicroseconde)
        {
            _o = null;
            _born = DateTime.Now;
            _delayInMicroseconde = delayInMicroseconde;
        }

        public bool Swap(Object pO)
        {
            DateTime now = DateTime.Now;
            if ( _o != pO 
                || (_o == pO && now.Subtract(_born).TotalMilliseconds > _delayInMicroseconde)) // _born.Subtract(now).TotalMilliseconds > 100))
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
