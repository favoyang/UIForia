namespace UIForia.Style.Parsing {

    public enum StyleTokenType {

        Invalid,

        Import,
        From,
        At,
        Use,
        Export,
        ExportAs,
        Const,
        Equal,
        Value,
        
        AttributeSpecifier,

        EndStatement,
        GreaterThan,
        LessThan,
        Colon,

        As,
        Dollar,

        // accessors
        Dot,
        Comma,
        // [
        BracesOpen,
        // ]
        BracesClose,
        BracketOpen,
        BracketClose,
        ParenOpen,
        ParenClose,

        // identifiers
        Identifier,
        VariableType,
        
        Rgba,
        Rgb,
        Url,

        // constants
        String,
        Boolean,
        Number,

        // booleans
        And,
        Not,
        
        // top level identifier block names
        Style,
        Animation,
        Texture,
        Audio,
        Cursor,
        
        BooleanAnd,
        BooleanOr,
        BooleanNot,
        
        // operators
        Plus,
        Minus,
        Times,
        Divide,
        Mod,
        // Comparators
        Equals,
        NotEquals,

        GreaterThanEqualTo,
        LessThanEqualTo,
        QuestionMark,
    }
}
