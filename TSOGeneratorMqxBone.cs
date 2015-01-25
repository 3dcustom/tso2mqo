using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public class TSOGeneratorMqxBone : TSOGenerator
    {
        public TSOGeneratorMqxBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        protected override bool DoLoadRefTSO(string path)
        {
            return true;
        }

        protected override bool DoGenerateMeshes()
        {
            return true;
        }
    }
}
