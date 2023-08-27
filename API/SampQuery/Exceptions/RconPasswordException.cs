namespace SAMPQuery 
{
	/// <summary>
	/// Represents errors that occurred during the validation of the RCON password
	/// </summary>
	public class RconPasswordException : System.Exception
	{
		/// <summary>
		/// Represents errors that occurred during the validation of the RCON password
		/// </summary>
		/// <param name="message">Error message. See RconPasswordExceptionMessages class</param>
		/// <returns>RconPasswordException instance</returns>
		public RconPasswordException(string message) : base(message) {}
	}
}