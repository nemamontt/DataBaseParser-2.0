using DataBaseParser.DTO;
using DataBaseParser.Enums;
using System;
using System.Collections.Generic;

namespace DataBaseParser.Contracts
{
    interface IDataBase
    {
        string Name { get; set; }
        DateTime LastUpdateDate { get; set; }
        MethodParsing ParsingMethod { get; set; }
        List<Vulnerability> Vulnerabilitys { get; set; }
    }
}
