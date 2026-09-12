namespace Bee.Base
{
    /// <summary>
    /// The basis the <see cref="DateTime"/> instants of a data set are expressed in, which decides what
    /// "now" has to mean when a value is written into or compared with them.
    /// </summary>
    /// <remarks>
    /// "Today" and "now" answer different questions. Today is a calendar day and belongs to the user's
    /// time zone on both sides (ADR-032 D12). Now is an instant, and an instant written into a data set
    /// has to share the basis of the instants already there: a client holds its data in the user's zone,
    /// because the connector converts each response into it, while the server holds the same data in
    /// UTC (ADR-032 D3). A user-zone reading written on the server is converted once more when the
    /// response reaches the client, and is stored off by the user's offset when the record is saved.
    /// </remarks>
    public enum DateTimeBasis
    {
        /// <summary>
        /// Instants are expressed in the user's time zone. This is the client side of the connector.
        /// </summary>
        UserZone,

        /// <summary>
        /// Instants are expressed in UTC. This is the server side, where data is read and written as stored.
        /// </summary>
        Utc,
    }
}
