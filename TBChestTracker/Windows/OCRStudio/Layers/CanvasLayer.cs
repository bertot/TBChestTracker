﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBChestTracker
{
    public class CanvasLayer : ICanvasLayer
    {
        public string Name { get; set; }
        public ICanvasShape Shape { get; set; }

    }
}
