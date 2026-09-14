// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Thrown when an operation can not proceed because the target resource is still depended upon by one or more other resources (for example, disposing or mutating a resource that is still part of a non-disposed <see cref="ResourceGroup"/>).
/// </summary>
public class ResourceDependencyException : Exception {
	/// <summary>
	/// Constructs a new <see cref="ResourceDependencyException"/> with no message.
	/// </summary>
	public ResourceDependencyException() { }
	/// <summary>
	/// Constructs a new <see cref="ResourceDependencyException"/> with the given <paramref name="message"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	public ResourceDependencyException(string? message) : base(message) { }
	/// <summary>
	/// Constructs a new <see cref="ResourceDependencyException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>.
	/// </summary>
	/// <param name="message">A message describing the error.</param>
	/// <param name="innerException">The exception that caused this exception.</param>
	public ResourceDependencyException(string? message, Exception? innerException) : base(message, innerException) { }

	internal static ResourceDependencyException CreateForPrematureDisposalOrMutation(string targetResourceType, string targetResourceName, ICollection<string> dependentResourceNames) {
		const int MaxResourcesToDisplay = 3;
		var joinedDependentResourceNames = String.Join(", ", dependentResourceNames.Take(MaxResourcesToDisplay).Select(n => $"'{n}'"));
		if (dependentResourceNames.Count > MaxResourcesToDisplay) joinedDependentResourceNames += ", ...";

		return new ResourceDependencyException(
			$"Can not execute this action (i.e. dispose or mutation) for {targetResourceType} '{targetResourceName}' because it is still in use by {dependentResourceNames.Count} other resource(s) " +
			$"({joinedDependentResourceNames}). Dispose or otherwise relinquish the dependency on those resources first before executing this action on '{targetResourceName}'."
		);
	}
}