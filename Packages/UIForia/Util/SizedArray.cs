using System;

namespace UIForia.Util {

    public struct ReadOnlySizedArray<T> {

        public readonly int size;
        public readonly T[] array;

        public ReadOnlySizedArray(in SizedArray<T> source) {
            this.size = source.size;
            this.array = source.array;
        }

        public ReadOnlySizedArray(int size, T[] array) {
            this.size = size;
            this.array = array;
        }

        public static implicit operator ReadOnlySizedArray<T>(SizedArray<T> source) {
            return new ReadOnlySizedArray<T>(source);
        }

        public static ReadOnlySizedArray<T> CopyFrom(T[] source, int size = -1) {
            
            if (source == null || size == 0) {
                return default;
            }
            
            if (size < 0) {
                size = source.Length;
            }
            T[] array = new T[size];
            for (int i = 0; i < size; i++) {
                array[i] = source[i];
            }
            return new ReadOnlySizedArray<T>(size, array);
        }

    }

    public struct SizedArray<T> {

        public int size;
        public T[] array;

        public SizedArray(int capacity) {
            this.size = 0;
            this.array = new T[capacity];
        }

        public SizedArray(T[] data) {
            this.size = data.Length;
            this.array = data;
        }

        public T this[int idx] {
            get => array[idx];
            set => array[idx] = value;
        }

        public T Add(in T item) {
            if (array == null) {
                array = new T[8];
            }
            else if (size + 1 >= array.Length) {
                Array.Resize(ref array, size + array.Length * 2);
            }

            array[size] = item;
            size++;
            return item;
        }

        public void AddRange(SizedArray<T> collection) {
            if (size + collection.size >= array.Length) {
                System.Array.Resize(ref array, size + collection.size * 2);
            }

            const int HandCopyThreshold = 5;
            if (collection.size < HandCopyThreshold) {
                int idx = size;
                for (int i = 0; i < collection.size; i++) {
                    array[idx++] = collection.array[i];
                }
            }
            else {
                System.Array.Copy(collection.array, 0, array, size, collection.size);
            }

            size += collection.size;
        }

        public void AddRange(ReadOnlySizedArray<T> collection) {
            if (size + collection.size >= array.Length) {
                System.Array.Resize(ref array, size + collection.size * 2);
            }

            const int HandCopyThreshold = 5;
            if (collection.size < HandCopyThreshold) {
                int idx = size;
                for (int i = 0; i < collection.size; i++) {
                    array[idx++] = collection.array[i];
                }
            }
            else {
                System.Array.Copy(collection.array, 0, array, size, collection.size);
            }

            size += collection.size;
        }

    }

}