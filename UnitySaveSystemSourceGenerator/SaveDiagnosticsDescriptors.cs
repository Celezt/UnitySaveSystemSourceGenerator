using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Celezt.SaveSystem.Generation
{
	public static class SaveDiagnosticsDescriptors
	{
		public static readonly DiagnosticDescriptor ClassMustBePartial = new(
					"CSS001",
					"A class containing the attribute '[Save]' must be partial",
					"The class '{0}' must be partial to allow the source generator to generate the necessary code for the save to work at '{1}'",
					"Save",
					DiagnosticSeverity.Error,
					isEnabledByDefault: true);
	}
}
