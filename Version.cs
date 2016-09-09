using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NadekoUpdater
{
    public class Version : IComparable<Version>
    {
        public string Value { get; private set; }

        public static readonly Version DefaultVersion = new Version("v0.0");

        [JsonConstructor]
        public Version(string value)
        {
            Value = value;
        }

        public int CompareTo(Version other)
        {
            var versions1 = Value.Split('.');
            var versions2 = other.Value.Split('.');
            var i = 0;
            do
            {
                if (versions1.Length <= i)
                {
                    if (versions2.Length <= i)
                        return 0;   //a.b.c ? a.b.c
                    return -1; //a.b.c ? a.b.c.d //minor version
                    
                }
                if (versions2.Length <= i)
                    return 1; //a.b.c.d ? a.b.c //minor version
                var com = string.Compare(versions1[i], versions2[i], StringComparison.Ordinal);
                if (com < 0)
                    return -1;
                if (com > 0)
                    return 1;
                //Else check minor version
                i++;
            } while (true);
        }

        public override string ToString()
        {
            return Value == "v0.0" ? "None" : Value;
        }
    }
}
