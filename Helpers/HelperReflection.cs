using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarryAnyone.Helpers
{
    static class HelperReflection
    {
        public static String Properties(Object o, String sep, BindingFlags flag)
        {
            String ret = null;
            if ((flag & BindingFlags.Instance) != 0)
            {
                PropertyInfo[] props = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (PropertyInfo prop in props)
                {
                    String add = null;
                    try
                    {
                        add = String.Format("{0} ?= {1}", prop.Name, prop.GetValue(o, null));
                    }
                    catch
                    {
                        add = String.Format("{0} READ ERROR", prop.Name);
                    }
                    if (ret == null)
                        ret = add;
                    else
                        ret += sep + add;
                }
            }
            if ((flag & BindingFlags.Static) != 0)
            {
                PropertyInfo[] props = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (PropertyInfo prop in props)
                {
                    String add = null;
                    try
                    {
                        add = String.Format("static {0} ?= {1}", prop.Name, prop.GetValue(null, null));
                    }
                    catch
                    {
                        add = String.Format("{0} READ ERROR", prop.Name);
                    }
                    if (ret == null)
                        ret = add;
                    else
                        ret += sep + add;
                }
            }
            return ret;
        }
    }
}
