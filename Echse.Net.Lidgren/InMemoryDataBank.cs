using System;
using System.Collections.Generic;
using System.Linq;
using Echse.Domain;
using Echse.Language;

namespace Echse.Net.Lidgren
{
    public class InMemoryDataBank : IEchseContext
    {
        private List<TagVariable> _variables = new();

        public InMemoryDataBank(string runCommand)
        {
            LanguageTick = TimeSpan.Zero;
        }

        public TimeSpan LanguageTick { get; }
        
        public IEnumerable<TagVariable> Variables => _variables;

        public void AddVariable(TagVariable variable)
        {
            var foundVars = _variables.Where(t => t.Id == variable.Id && 
                                                  t.DataTypeSymbol == variable.DataTypeSymbol &&
                                                  t.Name == variable.Name &&
                                                  t.Scope == variable.Scope);
            if(foundVars != null && !foundVars.Any())
                _variables.Add(variable);
            else
            {
                foreach (var foundVar in foundVars)
                    foundVar.Value = variable.Value;
            }
        }

        public void RemoveTagByName(string tagName)
        {
            var variablesFound = _variables.Where(v => v.Name == tagName &&
                                                        v.DataTypeSymbol == LexiconSymbol.TagDataType);
            if(variablesFound.Any())
                _variables = _variables.Except(variablesFound).ToList();
        }
        
        public void RemoveTagByNameAndScope(string tagName, string scope)
        {
            var variablesFound = _variables.Where(v => v.Scope == scope && 
                                                                 v.Name == tagName &&
                                                                 v.DataTypeSymbol == LexiconSymbol.TagDataType);
            if(variablesFound.Any())
                _variables = _variables.Except(variablesFound).ToList();
        }
    }
}