using UnityEngine;

namespace Src.Text {

    public struct WordInfo {

        public Vector2 position;
        public Vector2 size;

        public int spaceStart;
        public int charCount;
        public int startChar;
        public float ascender;
        public float descender;
        public float xAdvance;
        public bool isNewLine;

        public int visibleCharCount => charCount - (charCount - spaceStart);
        
    }

}