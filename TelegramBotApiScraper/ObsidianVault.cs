using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotApiScraper
{
    static internal class ObsidianVault
    {
        static private HashSet<string> _primitives;

        static private Dictionary<string, ApiUnit> _stubs;

        static private Dictionary<string, ApiUnit> _records;

        static private Dictionary<string, ApiUnit> _unions;

        static private Dictionary<string, ApiUnit> _methods;

        static internal void Create(
            string vaultPath,
            List<ApiUnit> units
        )
        {
            Sort(units);
        }

        static private void Sort(List<ApiUnit> units)
        {
            _primitives = new();
            _stubs = new();
            _records = new();
            _unions = new();
            _methods = new();

            foreach (var unit in units)
            {
                if (char.IsUpper(unit.Name[0]))
                {
                    if (unit.Units.Count == 0)
                    {
                        _stubs.Add(unit.Name, unit);
                    }
                    else
                    {
                        if (unit.Units[0].TypeName != string.Empty)
                        {
                            _records.Add(unit.Name, unit);
                        }
                        else
                        {
                            _unions.Add(unit.Name, unit);
                        }
                    }
                }
                else
                {
                    _methods.Add(unit.Name, unit);
                }
            }

            foreach (var (_, unit) in _records.Union(_methods))
            {
                foreach (var field in unit.Units)
                {
                    _primitives.Add(field.TypeName);
                }
            }
        }
    }
}
