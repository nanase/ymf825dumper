﻿using System;
using System.Collections.Generic;

namespace Ymf825Dumper
{
    [Serializable]
    public class Project
    {
        #region -- Public Properties --

        public IEnumerable<ToneItem> Tones { get; set; }

        public IEnumerable<EqualizerItem> Equalizers { get; set; }

        #endregion
    }
}
