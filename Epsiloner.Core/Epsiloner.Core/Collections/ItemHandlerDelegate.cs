namespace Epsiloner.Collections
{
    ///<summary>
    ///
    ///</summary>
    ///<typeparam name="T"></typeparam>
    ///<param name="inserted">Indicates if item has been inserted or removed</param>
    ///<param name="item">Item which has been inserter or removed</param>
    public delegate void ItemHandlerDelegate<T>(bool inserted, T item, int index);
}