using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadianciaKS.Application.DTOs.CashShift
{
    public class ExportFileDto
    {
        public byte[] Content { get; set; } = [];
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}