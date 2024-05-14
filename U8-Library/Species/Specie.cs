using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U8_Library.Species
{
    public class Specie
    {

        public int SpeciesID { get; private set; }
        public string Name { get; private set; }

        public Specie(int id, string name)
        {
            SpeciesID = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{SpeciesID};{Name}";
        }
    }
}
