// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Resources;

public class ResourceDependencyException : ApplicationException {
	public ResourceDependencyException() { }
	public ResourceDependencyException(string? message) : base(message) { }
	public ResourceDependencyException(string? message, Exception? innerException) : base(message, innerException) { }

	internal static ResourceDependencyException CreateForPrematureDisposal(string targetResourceType, string targetResourceName, ICollection<string> dependentResourceNames) {
		const int MaxResourcesToDisplay = 3;
		var joinedDependentResourceNames = String.Join(", ", dependentResourceNames.Take(MaxResourcesToDisplay).Select(n => $"'{n}'"));
		if (dependentResourceNames.Count > MaxResourcesToDisplay) joinedDependentResourceNames += ", ...";

		return new ResourceDependencyException(
			$"Can not dispose of {targetResourceType} resource '{targetResourceName}' because it is still in use by {dependentResourceNames.Count} other resource(s) " +
			$"({joinedDependentResourceNames}). Dispose those resources first before disposing of the parent {targetResourceType}."
		);
	}
}