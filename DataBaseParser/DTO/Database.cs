using DataBaseParser.Contracts;
using DataBaseParser.Core;
using DataBaseParser.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataBaseParser.DTO
{
    public class Database : ObservedObject, IDataBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private DateTime _lastUpdateDate;
        public DateTime LastUpdateDate
        {
            get => _lastUpdateDate;
            set
            {
                _lastUpdateDate = value;
                OnPropertyChanged();
            }
        }

        private MethodParsing _parsingMethod;
        public MethodParsing ParsingMethod
        {
            get => _parsingMethod; 
            set
            {
                _parsingMethod = value;
                OnPropertyChanged();
            }
        }

        public List<Vulnerability> Vulnerabilitys { get; set; }
    }

    public class Vulnerability : ObservedObject, IVulnerability
    {
        private string _Identifier;
        public string Identifier
        {
            get =>_Identifier; 
            set 
            {
                _Identifier = value;
                OnPropertyChanged();
            }
        }

        private Dictionary<string, string> _parameterAndDescription;
        public Dictionary<string, string> ParameterAndDescription
        {
            get => _parameterAndDescription;
            set 
            {
                _parameterAndDescription = value; 
                OnPropertyChanged() ;
            }
        }

        private List<string> _reference;
        public List<string> Reference
        {
            get => _reference; 
            set 
            {
                _reference = value;
                OnPropertyChanged();
            }
        }
    }
}
