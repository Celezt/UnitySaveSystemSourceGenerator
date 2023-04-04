using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Celezt.SaveSystem.Generation
{
	internal static class SaveDiagnosticsDescriptors
	{
		public static readonly DiagnosticDescriptor ClassMustBePartial = new(
			"CSS001",
			"A class containing the attribute '[Save]' must be partial",
			"The class '{0}' must be partial to allow the source generator to generate the necessary code for the save to work",
			"Save",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor MustBeInsideAClass = new(
			"CSS002",
			"The attribute '[Save]' must be inside a class",
			"The use of '[Save]' inside a '{0}' is currently not supported",
			"Save",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor MustImplementIIdentifiable = new(
			"CSS003",
			"A class containing the attribute '[Save]' must implement IIdentifiable",
			"The class '{0}' must implement IIdentifiable for the save to be identified. If the class is derived from MonoBehaviour, then it is expected to have a parent containing the behaviour 'SaveBehaviour'",
			"Save",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);
	}
}
