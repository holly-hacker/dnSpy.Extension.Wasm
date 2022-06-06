using System;
using System.Collections.Generic;

namespace dnSpy.Extension.Wasm;

internal static class Utils
{
	public static TValue GetOrInsert<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, Func<TValue> insertValueFactory)
		where TKey : notnull
	{
		if (dic.TryGetValue(key, out var found))
			return found!;

		dic[key] = found = insertValueFactory();
		return found;
	}
}
