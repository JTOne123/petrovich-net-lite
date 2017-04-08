﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPetrovichLite
{
    [Serializable]
    internal sealed class ParseException : Exception
    {
        internal ParseException(string message)
            : base(message)
        {

        }
    }
}
