﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.RepoTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            var translator = new RepoTranslator();
            translator.GetHarmonizeRepos().ToArray();
        }
    }
}