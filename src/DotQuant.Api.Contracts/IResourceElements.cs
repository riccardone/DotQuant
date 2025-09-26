namespace DotQuant.Api.Contracts
{
    /// <summary>
    /// Given a resource, returns the constituents elements that make up the resource 
    /// </summary>
    public interface IResourceElements
    {
        /// <summary>
        /// Splits the given resource into clientname, version and filename and returns 
        /// it as a tuple
        /// </summary>
        /// <param name="uri">Format is: client/version/file.json</param>
        /// <returns>a tuple containing: (clientname) </returns>
        Tuple<string, string> GetResourceElements(string uri);
    }
}
