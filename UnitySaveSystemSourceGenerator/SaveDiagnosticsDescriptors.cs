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
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor MustImplementIIdentifiable = new(
			"CSS003",
			"A class containing the attribute '[Save]' must implement IIdentifiable",
			"The class '{0}' must implement IIdentifiable for the save to work. If the class is only derived from MonoBehaviour, then it is assumed to be a child of 'SaveBehaviour'",
			"Save",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor GetMethodMustReturnAndNoParameters = new(
			"CSS004",
			"A get method must return a value and have no parameters",
			"The method '{0}' must return a value and contain no parameters to be a valid get method",
			"Save",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor SetMethodMustBeVoidAndHaveParameters = new(
			"CSS005",
			"A set method must be void and only contain one parameter",
			"The method '{0}' must be void and only contain one parameter to be a valid set method",
			"Save",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor MustCallRegisterSaveObjectInvocation = new(
			"CSS006",
			"The method 'RegisterSaveObject' must be called for the 'SaveAttribute' to work",
			"The class '{0}' must call the method 'RegisterSaveObject' to register all existing 'SaveAttribute' inside the class. It is recommended to call from Awake()",
			"Save",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);
	}
}
