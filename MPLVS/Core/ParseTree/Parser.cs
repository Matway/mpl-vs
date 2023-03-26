// This file was generated on Sat Jul 1, 2017 20:11 (UTC+03) by REx v5.45 which is Copyright (c) 1979-2017 by Gunther Rademacher <grd@gmx.net>
// REx command line: mplGrammar_2017Jul01_reduced-extended.ebnf -tree -ll 2 -faster -csharp

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MPLVS {
  public class Parser {
    public struct CompoundStart {
      public string Name;
      public int Begin, Line, Column;

      public CompoundStart(string name, int begin, int line, int column) {
        this.Name   = name;
        this.Begin  = begin;
        this.Line   = line;
        this.Column = column;
      }
    }

    public struct CompoundEnd {
      public string Name;
      public int End;

      public CompoundEnd(string name, int end) {
        this.Name = name;
        this.End  = end;
      }
    }

    public struct TerminalStart {
      public string Name;
      public int Begin, End, Line, Column;

      public TerminalStart(string name, int begin, int end, int line, int column) {
        this.Name   = name;
        this.Begin  = begin;
        this.End    = end;
        this.Line   = line;
        this.Column = column;
      }
    }

    public event EventHandler Reset;
    public event EventHandler Compleate;
    public event EventHandler<CompoundStart> StartCompound;
    public event EventHandler<CompoundEnd> EndCompound;
    public event EventHandler<TerminalStart> Terminal;
    public event EventHandler<SyntaxError> PushError;

    public class SyntaxError {
      internal int Begin { get; set; }
      internal int End { get; set; }
      internal string Token;
      internal string RawMessage;

      public SyntaxError(int begin, int end, string token, string message) {
        this.Begin      = begin;
        this.End        = end;
        this.Token      = token;
        this.RawMessage = message;
      }

      public string Message =>
        $" | Pos: {Begin.ToString(CultureInfo.InvariantCulture)}, Error message: \"{RawMessage}\" in token: {Token}";
    }

    public struct PositionInfo {
      public PositionInfo(int cursor = 0, int line = 0, int lineBegin = 0) {
        Debug.Assert(cursor >= 0 && line >= 0 && lineBegin >= 0);
        Debug.Assert(lineBegin <= cursor);

        Cursor    = cursor;
        Line      = line;
        LineBegin = lineBegin;
      }

      public int Cursor;

      /// <summury>NOTE: Zero-based.</summury>
      public int Line;

      public int LineBegin;

      /// <summury>NOTE: Zero-based.</summury>
      public int Column {
        get {
          var result = Cursor - LineBegin;
          Debug.Assert(result >= 0);

          return result;
        }
      }
    }

    public Parser() { }

    public void Initialize(string s) {
      Input = s ?? throw new ArgumentNullException(nameof(s));
      Size  = Input.Length;

      ResetState();
    }

    public void ResetState() {
      this.Pos = new PositionInfo();

      this.Reset?.Invoke(this, null);
    }

    private PositionInfo Pos = new PositionInfo();
    private string Input     = null;
    private int Size         = 0;

    private bool HasInput => Pos.Cursor < Size;
    private char CurChar  => Input[Pos.Cursor];
    private bool HasNext  => Pos.Cursor + 1 < Size;
    private char NextChar => Input[Pos.Cursor + 1];
    private bool AtEnd    => !HasInput;

    private static string FormatExpected(string expected, string got) =>
      "Got:\t\t" + got + "\nExpected:\t" + expected;

    private bool CurPairIs(char a, char b) => CurChar == a && NextChar == b;

    private bool CurIsAnyOf(in string set) => set.IndexOf(CurChar) >= 0;

    private void Advance(bool linesAsTerminals = true) {
      Debug.Assert(!AtEnd, "Attempt to advance the empty buffer");

      ++Pos.Cursor;
      ProcessNewLinesIfAny(linesAsTerminals);
    }

    private void AdvanceAtCurrent(bool linesAsTerminals = true) {
      var start = Pos.Cursor;

      ProcessNewLinesIfAny(linesAsTerminals);
      if (Pos.Cursor == start) {
        Debug.Assert(!AtEnd, "Attempt to advance the empty buffer");

        ++Pos.Cursor;
      }
    }

    private void ProcessNewLinesIfAny(bool linesAsTerminals = true) {
      while (HasInput && (CurChar == '\n' || CurChar == '\r')) {
        ProcessNewLineIfAny(linesAsTerminals);
      }

      void ProcessNewLineIfAny(bool lineAsTerminal) {
        switch (CurChar) {
          case '\n': {
            var start = Pos;
            ++Pos.Line;
            Pos.LineBegin = ++Pos.Cursor;
            if (lineAsTerminal) {
              Terminal?.Invoke(this, new TerminalStart("LF", start.Cursor, Pos.Cursor, start.Line, start.Column));
            }
            break;
          }

          case '\r': {
            if (HasNext && NextChar == '\n') {
              var start = Pos;
              ++Pos.Line;
              Pos.LineBegin = Pos.Cursor += 2;
              if (lineAsTerminal) {
                Terminal?.Invoke(this, new TerminalStart("CRLF", start.Cursor, Pos.Cursor, start.Line, start.Column));
              }
            }
            else {
              ProcessOrphanCR();
            }
            break;
          }
        }

        void ProcessOrphanCR() {
          Error("CR", "CR (aka \\r) that not followed by LF (aka \\n) is not allowed");
          ++Pos.Line;
          Pos.LineBegin = ++Pos.Cursor;
        }
      }
    }

    public void ParseProgramWithEOF() {
      StartCompound?.Invoke(this, new CompoundStart("ProgramWithEOF", Pos.Cursor, Pos.Line, Pos.Column));
      if (Size != 0) {
        ParseProgram(() => HasInput);
      }

      Terminal?.Invoke(this, new TerminalStart("EOF", Size, Size, Pos.Line, Pos.Column));
      EndCompound?.Invoke(this, new CompoundEnd("ProgramWithEOF", Size));

      Compleate?.Invoke(this, null);
    }

    private void ParseProgram(Func<bool> terminatorIsNotReached) {
      StartCompound?.Invoke(this, new CompoundStart("Program", Pos.Cursor, Pos.Line, Pos.Column));
      if (HasInput && IsWS(CurChar)) {
        ParseWhitespaces();
      }

      if (terminatorIsNotReached()) {
        ParseExpression();
        // now parsing expressionWithLeadingWS
        while (terminatorIsNotReached()) {
          if (IsWS(CurChar)) {
            ParseWhitespaces();
            if (terminatorIsNotReached()) {
              ParseExpression();
            }
            else {
              break;
            }
          }
          else if (terminatorIsNotReached()) {
            ParseNonWSSeparableExpression();
          }
        }
      }

      EndCompound?.Invoke(this, new CompoundEnd("Program", Pos.Cursor));

      void ParseWhitespaces() {
        StartCompound?.Invoke(this, new CompoundStart("Whitespaces", Pos.Cursor, Pos.Line, Pos.Column));
        while (HasInput && IsWS(CurChar)) {
          AdvanceAtCurrent();
        }

        EndCompound?.Invoke(this, new CompoundEnd("Whitespaces", Pos.Cursor));
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWS(char ch) =>
      ch == '\t' || ch == '\n' || ch == '\r' || ch == ' ';

    private void ParseExpression() {
      StartCompound?.Invoke(this, new CompoundStart("Expression", Pos.Cursor, Pos.Line, Pos.Column));

      if (CurIsAnyOf("#(.[{")) {
        ParseNonWSSeparableExpression();
      }
      else {
        ParseWSSeparableExpression();
      }

      EndCompound?.Invoke(this, new CompoundEnd("Expression", Pos.Cursor));
    }

    private void ParseWSSeparableExpression() {
      StartCompound?.Invoke(this, new CompoundStart("WSSeparableExpression", Pos.Cursor, Pos.Line, Pos.Column));
      var cur = CurChar;
      if (cur == '"' || cur == '«') {
        ParseString();
      }
      else if (cur == '+' || cur == '-') {
        if (HasNext && IsDigit(NextChar)) {
          ParseNumber();
        }
        else {
          ParseLabelOrNameExpression();
        }
      }
      else if (IsDigit(cur)) {
        ParseNumber();
      }
      else if (IsMplLetter(cur) || cur == '.') {
        ParseLabelOrNameExpression();
      }
      else if (cur == '!' || cur == '@') {
        if (HasNext && NextChar == ':') {
          ParseLabelOrNameExpression();
        }
        else {
          ParseNameExpression();
        }
      }
      else {
        var start = Pos;
        SkipNonWhiteSpaces();
        Error(start.Cursor, Pos.Cursor, "WSSeparableExpression", "There is nothing that WSSeparableExpression can contain");
      }

      EndCompound?.Invoke(this, new CompoundEnd("WSSeparableExpression", Pos.Cursor));

      void SkipNonWhiteSpaces() =>
        Skip(a => a != ' ' && a != '\n' && a != '\r'); // FIXME: Should we check the tabulation too?
    }

    private void ParseNonWSSeparableExpression() {
      StartCompound?.Invoke(this, new CompoundStart("NonWSSeparableExpression", Pos.Cursor, Pos.Line, Pos.Column));
      switch (CurChar) {
        case '{': ParseBlock("Object", "'{'", "'}'", '}'); break;
        case '(': ParseBlock("List",   "'('", "')'", ')'); break;
        case '[': ParseBlock("Code",   "'['", "']'", ']'); break;

        case '.': ParseMemberNameExpression(); break;
        case '#': ParseComment();              break;

        default:
          Error("NonWSSeparableExpression", "There is nothing that NonWSSeparableExpression can contain");
          ++Pos.Cursor;
          break;
      }
      EndCompound?.Invoke(this, new CompoundEnd("NonWSSeparableExpression", Pos.Cursor));
    }

    private void ParseBlock(string symbol, string starter, string terminator, char terminatorSign) =>
      ParseBlock(symbol, starter, terminator, terminatorSign, Pos, () => { });

    private void ParseLabel(PositionInfo start) {
      ParseBlock("Label", "':'", "';'", ';', start, () => {
        Terminal?.Invoke(this, new TerminalStart("Name", start.Cursor, Pos.Cursor, start.Line, start.Column));
      });
    }

    private void ParseBlock(string symbol, string starter, string terminator, char terminatorSign, PositionInfo start, Action f) {
      StartCompound?.Invoke(this, new CompoundStart(symbol, start.Cursor, start.Line, start.Column));
      f();

      Terminal?.Invoke(this, new TerminalStart(starter, Pos.Cursor, ++Pos.Cursor, Pos.Line, Pos.Column - 1));
      ParseProgram(() => HasInput && CurChar != terminatorSign);
      if (HasInput && CurChar == terminatorSign) {
        Terminal?.Invoke(this, new TerminalStart(terminator, Pos.Cursor, ++Pos.Cursor, Pos.Line, Pos.Column - 1));
      }
      else {
        Error(start.Cursor, symbol, FormatExpected("A tail of the " + symbol + " with the " + symbol + " terminator '" + terminatorSign + '\'', "The unterminated " + symbol));
      }

      EndCompound?.Invoke(this, new CompoundEnd(symbol, Pos.Cursor));
    }

    private void ParseMemberNameExpression() {
      StartCompound?.Invoke(this, new CompoundStart("MemberNameExpression", Pos.Cursor, Pos.Line, Pos.Column));
      var start = Pos;
      ++Pos.Cursor; // "."
      if (HasInput) {
        switch (CurChar) {
          case '@':
            ++Pos.Cursor;
            ParseMemberName();
            Terminal?.Invoke(this, new TerminalStart("NameReadMember", start.Cursor, Pos.Cursor, start.Line, start.Column));
            break;
          case '!':
            ++Pos.Cursor;
            ParseMemberName();
            Terminal?.Invoke(this, new TerminalStart("NameWriteMember", start.Cursor, Pos.Cursor, start.Line, start.Column));
            break;
          default:
            ParseMemberName();
            Terminal?.Invoke(this, new TerminalStart("NameMember", start.Cursor, Pos.Cursor, start.Line, start.Column));
            break;
        }
      }

      EndCompound?.Invoke(this, new CompoundEnd("MemberNameExpression", Pos.Cursor));
    }

    private void ParseNameExpression() {
      StartCompound?.Invoke(this, new CompoundStart("NameExpression", Pos.Cursor, Pos.Line, Pos.Column));
      switch (CurChar) {
        case '@': parse("NameRead");  break;
        case '!': parse("NameWrite"); break;
      }
      EndCompound?.Invoke(this, new CompoundEnd("NameExpression", Pos.Cursor));

      void parse(string access) {
        var start = Pos;
        ++Pos.Cursor; // "@" or "!".
        if (HasInput && (CurIsAnyOf("!+-.@") || IsMplLetter(CurChar))) {
          ParseName();
          Terminal?.Invoke(this, new TerminalStart(access, start.Cursor, Pos.Cursor, start.Line, start.Column));
        }
        else {
          Terminal?.Invoke(this, new TerminalStart("Name", start.Cursor, Pos.Cursor, start.Line, start.Column));
        }
      }
    }

    private void ParseString() {
      if (CurChar == '"') {
        var start = Pos;
        Advance(false); // Opening of the string '"', also maybe skip new lines.

        ParseStringBody();

        if (AtEnd) {
          Error(start.Cursor, "String", FormatExpected("A tail of the string with the string terminator '\"'", "The unterminated string"));
        }
        else {
          ++Pos.Cursor; // Closing of the string '"'.
        }
        Terminal?.Invoke(this, new TerminalStart("String", start.Cursor, Pos.Cursor, start.Line, start.Column));
      }
      else {
        var start = Pos;
        ParseRawString();
        // NOTE: We dubbed it a string, but not a raw-string, so the other extension's parts will not differentiate them.
        Terminal?.Invoke(this, new TerminalStart("String", start.Cursor, Pos.Cursor, start.Line, start.Column));
      }

      void ParseStringBody() {
        while (HasInput && CurChar != '"') {
          if (CurChar == '\\') {
            var start = Pos;
            Advance(false);
            if (Pos.Cursor == start.Cursor + 1) {
              ParseStringEscapeSequenceTail();
            }
            else {
              Error(start.Cursor + 1, "String", FormatExpected("A tail of the escape sequence", "The new line"));
            }
          }
          else {
            Advance(false);
          }
        }
      }

      void ParseStringEscapeSequenceTail() {
        if (AtEnd) {
          Error("String", FormatExpected("A body of the escape sequence", "The end of input"));
          return;
        }

        char[] escapeTails = { '\"', '\\', 'n', 'r', 't' }; // FIXME: GC.
        if (Array.IndexOf<char>(escapeTails, CurChar) >= 0 || IsUpperCaseHexDigit(CurChar)) {
          Advance(false);
        }
        else {
          Error("String", FormatExpected("One of '\"', '\\', 'n', 'r', 't', or upper case hexadecimal digit", "The escape sequence body which is invalid"));
          Advance(false);
        }
      }

      void ParseRawString() {
        var start = Pos;
        ++Pos.Cursor; // "«".
        while (HasInput && CurChar != '»') {
          if (CurChar == '«') {
            ParseRawString();
          }
          else {
            AdvanceAtCurrent(false);
          }
        }

        if (AtEnd) {
          Error(start.Cursor, "RawString", FormatExpected("A tail of the raw string with the string terminator '»'", "The unterminated string"));
        }
        else {
          ++Pos.Cursor; // "»".
        }
      }
    }

    private void ParseNumber() {
      var start  = Pos;
      var signed = false;
      var isOk   = true;
      if (HasNext && CurPairIs('0', 'x')) {
        Pos.Cursor += 2;
        if (HasInput && IsHexDigit(CurChar)) {
          Skip(a => IsHexDigit(a));
        }
        else {
          Error("Number", "There is a hex number without a digits after \'0x\' ");
          isOk = false;
        }
      }
      else if (CurChar == '+' || CurChar == '-') {
        signed = true;
        ++Pos.Cursor;
        if (CurChar == '0') {
          ++Pos.Cursor;
        }
        else {
          Skip(a => IsDigit(a));
        }
      }
      else {
        if (CurChar == '0') {
          ++Pos.Cursor;
        }
        else {
          Skip(a => IsDigit(a));
        }
      }

      if (isOk && HasInput && CurIsAnyOf(".Ee")) {
        ParseReal(start);
      }
      else if (isOk && HasInput && (CurChar == 'n' || CurChar == 'i')) {
        ParseInteger(start, signed);
      }
      else if (isOk) {
        Terminal?.Invoke(this, new TerminalStart("Number", start.Cursor, Pos.Cursor, start.Line, start.Column));
      }
    }

    private void ParseInteger(PositionInfo start, bool signed) {
      var isOk = true;
      if (signed) {
        if (HasInput && CurChar == 'n') {
          Error(start.Cursor, "Number", "n-numbers can't have sign");
          isOk = false;
        }
        else if (HasInput && CurChar == 'i') {
          ParseSuffix("There is nothing like x, 8, 16, 32 or 64 after i");
        }
      }
      else if (HasInput && (CurChar == 'i' || CurChar == 'n')) {
        ParseSuffix("There is nothing like x, 8, 16, 32 or 64 after i or n");
      }

      if (isOk) {
        Terminal?.Invoke(this, new TerminalStart("Number", start.Cursor, Pos.Cursor, start.Line, start.Column));
      }

      void ParseSuffix(string messageOnError) {
        ++Pos.Cursor;
        if (HasInput) {
          if (CurChar == '8' || CurChar == 'x') {
            ++Pos.Cursor;
          }
          else if (HasNext) {
            if (CurPairIs('3', '2') || CurPairIs('1', '6') || CurPairIs('6', '4')) {
              Pos.Cursor += 2;
            }
            else {
              OutError("Number", messageOnError, out isOk);
            }
          }
          else {
            OutError("Number", messageOnError, out isOk);
          }
        }
        else {
          OutError("Number", messageOnError, out isOk);
        }
      }
    }

    private void ParseReal(PositionInfo start) {
      var isOk = true;
      if (CurChar == '.') {
        ++Pos.Cursor;
        if (HasInput && IsDigit(CurChar)) {
          Skip(a => IsDigit(a));
        }
        else { // TODO: Refactoring.
          // FIXME: `0.Message` - is it MPL-grammar violation?.

          if (HasInput && IsMplLetter(CurChar)) {
            var errStart = Pos.Cursor;
            SkipPseudoName();
            Error(errStart, Pos.Cursor, "Real", "There must be a digit after point");
            isOk = false;

            void SkipPseudoName() =>
              Skip(a => IsDigit(a) || IsMplLetter(a) || CurIsAnyOf("!+-.@"));
          }
          else {
            OutError("Real", "There must be a digit after point", out isOk);
          }
        }

        if (HasInput && (CurChar == 'e' || CurChar == 'E')) {
          ParseExponent();
        }
      }
      else if (CurChar == 'e' || CurChar == 'E') {
        ParseExponent();
      }
      else {
        OutError("Real", "There is an invalid character in the token", out isOk);
      }

      if (HasInput && CurChar == 'r') {
        ++Pos.Cursor;
        if (HasNext && (CurPairIs('3', '2') || CurPairIs('6', '4'))) {
          Pos.Cursor += 2;
        }
        else {
          OutError("Real", "There is nothing like 32 or 64 after r", out isOk);
        }
      }

      if (isOk) {
        Terminal?.Invoke(this, new TerminalStart("Real", start.Cursor, Pos.Cursor, start.Line, start.Column));
      }

      void ParseExponent() {
        ++Pos.Cursor;
        if (HasInput && (CurChar == '+' || CurChar == '-')) {
          ++Pos.Cursor;
          if (HasInput && CurChar == '0') {
            ++Pos.Cursor;
          }
          else if (HasInput && IsDigit(CurChar)) {
            Skip(a => IsDigit(a));
          }
          else {
            OutError("Real", "There must be a digit after \'E\'", out isOk);
          }
        }
        else if (HasInput && CurChar == '0') {
          ++Pos.Cursor;
        }
        else if (HasInput && IsDigit(CurChar)) {
          Skip(a => IsDigit(a));
        }
        else {
          OutError("Real", "There must be a digit after \'E\'", out isOk);
        }
      }
    }

    private void OutError(string token, string message, out bool isOk) {
      Error(token, message);
      isOk = false;
    }

    private void ParseComment() {
      var start = Pos;
      ++Pos.Cursor; // "#".

      var lastNonWS = Pos.Cursor; // FIXME: This is not a "last-non-white-space".
      while (HasInput && CurChar != '\n' && CurChar != '\r') {
        if (CurChar == ' ' || CurChar == '\t') {
          ++Pos.Cursor;
        }
        else {
          ++Pos.Cursor;
          lastNonWS = Pos.Cursor;
        }
      }

      Terminal?.Invoke(this, new TerminalStart("Comment", start.Cursor, lastNonWS, start.Line, start.Column));
    }

    private void ParseMemberName() {
      if (HasInput && (CurChar == '+' || CurChar == '-')) {
        ++Pos.Cursor;
        if (HasInput && (IsMplLetter(CurChar) || CurChar == '+' || CurChar == '-')) {
          ++Pos.Cursor;
          SkipLettersDigitsSigns();
        }
        else {
          return;
        }
      }
      else if (HasInput && IsMplLetter(CurChar)) {
        ++Pos.Cursor;
        SkipLettersDigitsSigns();
      }
      else {
        Error("MemberName", "It must be MemberName here, but it's empty");
      }

      void SkipLettersDigitsSigns() =>
        Skip(a => IsMplLetter(a) || IsDigit(a) || a == '+' || a == '-');
    }

    private void ParseLabelOrNameExpression() {
      var start = Pos;
      ParseName();
      if (HasInput && CurChar == ':') {
        ParseLabel(start);
      }
      else {
        StartCompound?.Invoke(this, new CompoundStart("NameExpression", start.Cursor, start.Line, start.Column));
        Terminal?.Invoke(this, new TerminalStart("Name", start.Cursor, Pos.Cursor, start.Line, start.Column));
        EndCompound?.Invoke(this, new CompoundEnd("NameExpression", Pos.Cursor));
      }
    }

    private void ParseName() {
      switch (CurChar) {
        case '@':
        case '!':
          ++Pos.Cursor;
          break;
        case '.':
          ++Pos.Cursor;
          Skip(a => a == '.');
          break;

        default:
          ParseMemberName();
          break;
      }
    }

    private void Skip(Predicate<char> f) {
      while (HasInput && f(CurChar)) {
        ++Pos.Cursor;
      }
    }

    private static bool IsUpperCaseHexDigit(char ch) => IsDigit(ch) || ch >= 'A' && ch <= 'F';

    private static bool IsHexDigit(char ch) => IsUpperCaseHexDigit(ch) || ch >= 'a' && ch <= 'f';

    public static bool IsMplLetter(char ch) => !IsDigit(ch) && "\t\n\r !\"#()+-.:;@[]{}".IndexOf(ch) < 0;

    public static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

    private void Error(int begin, int end, string token, string message) =>
      PushError?.Invoke(this, new SyntaxError(begin, end, token, message));

    private void Error(int position, string token, string message) =>
      Error(position, position, token, message);

    private void Error(string token, string message) =>
      Error(Pos.Cursor, token, message);
  }
}