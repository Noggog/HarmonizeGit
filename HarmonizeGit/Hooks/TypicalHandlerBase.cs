﻿using FishingWithGit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public abstract class TypicalHandlerBase
    {
        protected HarmonizeGitBase harmonize;
        public bool NeedsConfig { get; protected set; } = true;
        public abstract IGitHookArgs Args { get; }

        public TypicalHandlerBase(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public abstract Task<bool> Handle();
    }
}
