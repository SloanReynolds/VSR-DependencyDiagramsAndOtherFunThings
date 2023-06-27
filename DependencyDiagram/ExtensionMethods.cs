using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class ExtensionMethods {
	public static T AddGetUnique<T>(this IList<T> list, Func<T, bool> predicate, Func<T> itemFunc) {
		if (list.Any(predicate)) {
			return list.Where(predicate).FirstOrDefault();
		}
		T item = itemFunc.Invoke();
		list.Add(item);

		return item;
	}
	public static void AddUnique<T>(this IList<T> list, T item) {
		if (list.Contains(item)) return;
		list.Add(item);
	}
}
