﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBChestTracker
{
    public interface IChestDataCollection
    {
        Dictionary<String, Dictionary<String,ChestDataItem>> Items { get; }
        void Build();
    }
}
