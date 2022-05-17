using MarryAnyone.MA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace MarryAnyone
{
#if NEWSAVE
    internal class MASaveDefiner : SaveableTypeDefiner
    {
        public MASaveDefiner() : base(2022160523) { }

        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(MAFamily), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(List<MAFamily>));
        }
    }
#endif
}
