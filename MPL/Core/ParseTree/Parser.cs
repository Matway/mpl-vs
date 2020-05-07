// This file was generated on Sat Jul 1, 2017 20:11 (UTC+03) by REx v5.45 which is Copyright (c) 1979-2017 by Gunther Rademacher <grd@gmx.net>
// REx command line: mplGrammar_Jul01.ebnf -tree -ll 2 -faster -csharp

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace MPL {
  public class MPLParser {
    public class ParseException : Exception {
      int position { get; set; }
      int line { get; set; }
      int column { get; set; }

      public ParseException(int p, int l, int c, string t, string m) {
        position = p;
        token = t;
        message = m;
        line = l + 1;
        column = c;
      }

      public String getMessage() {

        return ("(" + line.ToString() + "," + column.ToString() + "): " +
          "Error message: " + '"' + message + '"' + " in token: " + token);
      }

      private string token, message;
    }

    public interface EventHandler {
      void reset(String s);
      void startNonterminal(String name, int begin, int line);
      void endNonterminal(String name, int end);
      void terminal(String name, int begin, int end, int line);
      void whitespace(int begin, int end);
      void pushError(int p, int l, int c, string t, string m);
      void getName(string name);
    }

    public MPLParser() { }

    public void initialize(String s, EventHandler eh) {
      eventHandler = eh;
      input = s;
      size = input.Length;
      reset(0, 0, 0);
    }

    public String getInput() {
      return input;
    }

    public void reset(int b, int e, int l) {
      begin = b;
      end = e;
      currentLine = l;
      eventHandler.reset(input);
    }

    public void reset() {
      reset(0, 0, 0);
    }

    public void parse_ProgramWithEOF() {
      eventHandler.startNonterminal("ProgramWithEOF", begin, currentLine);
      if (size != 0) {
        parse_Program();
      }

      eventHandler.terminal("EOF", size, size, currentLine);
      eventHandler.endNonterminal("ProgramWithEOF", size);
    }

    private int begin = 0, end = 0, size = 0, currentLine = 0;
    private EventHandler eventHandler = null;
    private String input = null;
    private bool isBuiltin = false;

    private void parse_Program(char ending) {
      eventHandler.startNonterminal("Program", begin, currentLine);
      if (end < size && (input[begin] == ' ' || input[begin] == '\n' || input[begin] == '\r' || input[begin] == '\t')) {
        parseTk_Whitespaces();
      }

      if (end < size && input[end] != ending) {
        parse_Expression();
        //now parsing expressionWithLeadingWS
        while (end < size && input[end] != ending) {
          if (input[begin] == ' ' || input[begin] == '\n' || input[begin] == '\r' || input[begin] == '\t') {
            parseTk_Whitespaces();
            if (end < size && input[end] != ending) {
              parse_Expression();
            } else {
              break;
            }
          } else {
            if (end < size && input[end] != ending) {
              parse_NonWSSeparableExpression();
            }
          }
        }
      }

      eventHandler.endNonterminal("Program", end);
      begin = end;
    }

    //initial program without args
    private void parse_Program() {
      eventHandler.startNonterminal("Program", begin, currentLine);
      if (end < size && (input[begin] == ' ' || input[begin] == '\n' || input[begin] == '\r' || input[begin] == '\t')) {
        parseTk_Whitespaces();
      }

      if (end < size) {
        parse_Expression();
        //now parsing expressionWithLeadingWS
        while (end < size) {
          if (input[begin] == ' ' || input[begin] == '\n' || input[begin] == '\r' || input[begin] == '\t') {
            parseTk_Whitespaces();
            if (end < size) {
              parse_Expression();
            } else {
              break;
            }
          } else {
            if (end < size) {
              parse_NonWSSeparableExpression();
            }
          }
        }
      }

      eventHandler.endNonterminal("Program", end);
      begin = end;
    }

    private void parse_Expression() {
      eventHandler.startNonterminal("Expression", begin, currentLine);
      if (input[begin] == ',' || input[begin] == '(' || input[begin] == '[' || input[begin] == '{' || input[begin] == '.' || input[begin] == '#') {
        parse_NonWSSeparableExpression();
      } else {
        parse_WSSeparableExpression();
      }

      eventHandler.endNonterminal("Expression", end);
      begin = end;
    }

    private void parse_WSSeparableExpression() {
      eventHandler.startNonterminal("WSSeparableExpression", begin, currentLine);
      char cur = input[end];
      if (cur == '"' || cur == '«') {
        parseTk_String();
      } else if (cur == '+' || cur == '-') {
        if (end + 1 < size && Char.IsDigit(input[end + 1])) {
          parse_Num();
        } else {
          parse_NameExpressionOrLableOrLableReset();
        }
      } else if (Char.IsDigit(cur)) {
        parse_Num();
      } else if (checkName(cur)) {
        parse_NameExpressionOrLableOrLableReset();
      } else if (cur == '!' || cur == '@') {
        if (end + 1 < size && input[end + 1] == ':') {
          parse_NameExpressionOrLableOrLableReset();
        } else {
          parse_NameExpression();
        }
      } else {
        throwException(end, "WSSeparableExpression", "There is nothing that WSSeparableExpression can contain");
        begin = end;
        findNextWhitespace();
        eventHandler.terminal("SomeError", begin, end, currentLine);
      }

      eventHandler.endNonterminal("WSSeparableExpression", end);
      begin = end;
    }

    private void parse_NonWSSeparableExpression() {
      eventHandler.startNonterminal("NonWSSeparableExpression", begin, currentLine);
      switch (input[begin]) {
        case '{':
        parse_Object();
        break;
        case '(':
        parse_List();
        break;
        case '[':
        parse_Code();
        break;
        case '.':
        parse_MemberNameExpression();
        break;
        case '#':
        parseTk_Comment();
        break;
        case ',':
        eventHandler.terminal("','", begin, ++end, currentLine);
        break;
        default:
        throwException(end, "NonWSSeparableExpression", "There is nothing that NonWSSeparableExpression can contain");
        ++end;
        eventHandler.terminal("SomeError", begin, end, currentLine);
        break;
      }

      eventHandler.endNonterminal("NonWSSeparableExpression", end);
      begin = end;
    }

    private void parse_MemberNameExpression() {
      eventHandler.startNonterminal("MemberNameExpression", begin, currentLine);
      ++end;
      if (end < size) {
        switch (input[end]) {
          case '@':
          ++end;
          parseTk_MemberName();
          eventHandler.terminal("NameReadMember", begin, end, currentLine);
          break;
          case '!':
          ++end;
          parseTk_MemberName();
          eventHandler.terminal("NameWriteMember", begin, end, currentLine);
          break;
          default:
          parseTk_MemberName();
          eventHandler.terminal("NameMember", begin, end, currentLine);
          break;
        }
      }

      eventHandler.endNonterminal("MemberNameExpression", end);
      begin = end;
    }

    private void parse_NameExpression() {
      eventHandler.startNonterminal("NameExpression", begin, currentLine);
      switch (input[end]) {
        case '@':
        ++end;
        if (end < size && (input[end] == '+' || input[end] == '-' || input[end] == '@' || input[end] == '!' || input[end] == '.' || checkLetter(input[end]))) {
          parse_Name();
          if (isBuiltin) {
            eventHandler.terminal("NameRead", begin, begin + 1, currentLine);
            eventHandler.terminal("Builtin", begin + 1, end, currentLine);
            isBuiltin = false;
          } else {
            eventHandler.terminal("NameRead", begin, end, currentLine);
          }
        } else {
          eventHandler.terminal("Builtin", begin, end, currentLine); //it's just name, then it's builtin
        }

        break;
        case '!':
        ++end;
        if (end < size && (input[end] == '+' || input[end] == '-' || input[end] == '@' || input[end] == '!' || input[end] == '.' || checkLetter(input[end]))) {
          parse_Name();
          if (isBuiltin) {
            eventHandler.terminal("NameWrite", begin, begin + 1, currentLine);
            eventHandler.terminal("Builtin", begin + 1, end, currentLine);
            isBuiltin = false;
          } else {
            eventHandler.terminal("NameWrite", begin, end, currentLine);
          }
        } else {
          eventHandler.terminal("Builtin", begin, end, currentLine); //it's just name, then it's builtin
        }

        break;
      }

      eventHandler.endNonterminal("NameExpression", end);
      begin = end;
    }

    private void parse_LabelReset() {
      eventHandler.startNonterminal("LabelReset", begin, currentLine);
      int b = begin;
      int l = currentLine;
      eventHandler.terminal("Name", begin, end, currentLine);
      begin = end;
      end += 2;
      eventHandler.terminal("':!'", begin, ++end, currentLine);
      begin = end;
      parse_Program(';');
      if (end < size && input[end] == ';') {
        eventHandler.terminal("';'", begin, ++end, currentLine);
      } else {
        throwException(b, l, "LabelReset", "Can't find ; in the end of a LabelReset");
      }

      eventHandler.endNonterminal("LabelReset", end);
      begin = end;
    }

    private void parse_Label() {
      eventHandler.startNonterminal("Label", begin, currentLine);
      int b = begin;
      int l = currentLine;
      eventHandler.terminal("Name", begin, end, currentLine);
      getName(begin, end); //get name for autocompletion list
      begin = end;
      eventHandler.terminal("':'", begin, ++end, currentLine);
      begin = end;
      parse_Program(';');
      if (end < size && input[end] == ';') {
        eventHandler.terminal("';'", begin, ++end, currentLine);
      } else {
        throwException(b, l, "Label", "Can't find ; in the end of a Label");
      }

      eventHandler.endNonterminal("Label", end);
      begin = end;
    }

    private void parse_Object() {
      eventHandler.startNonterminal("Object", begin, currentLine);
      int b = begin;
      int l = currentLine;
      eventHandler.terminal("'{'", begin, ++end, currentLine);
      begin = end;
      parse_Program('}');
      if (end < size && input[end] == '}') {
        eventHandler.terminal("'}'", begin, ++end, currentLine);
      } else {
        throwException(b, l, "Object", "Can't find } in the end of an Object");
      }

      eventHandler.endNonterminal("Object", end);
      begin = end;
    }

    private void parse_List() {
      eventHandler.startNonterminal("List", begin, currentLine);
      int b = begin;
      int l = currentLine;
      eventHandler.terminal("'('", begin, ++end, currentLine);
      begin = end;
      parse_Program(')');
      if (end < size && input[end] == ')') {
        eventHandler.terminal("')'", begin, ++end, currentLine);
      } else {
        throwException(b, l, "List", "Can't find ) in the end of a List");
      }

      eventHandler.endNonterminal("List", end);
      begin = end;
    }

    private void parse_Code() {
      eventHandler.startNonterminal("Code", begin, currentLine);
      int b = begin;
      int l = currentLine;
      eventHandler.terminal("'['", begin, ++end, currentLine);
      begin = end;
      parse_Program(']');
      if (end < size && input[end] == ']') {
        eventHandler.terminal("']'", begin, ++end, currentLine);
      } else {
        throwException(b, l, "Code", "Can't find ] in the end of a Code");
      }

      eventHandler.endNonterminal("Code", end);
      begin = end;
    }

    private void parseTk_Whitespaces() {
      eventHandler.startNonterminal("Whitespaces", begin, currentLine);
      char current = input[begin];
      while (current == ' ' || current == '\n' || current == '\r' || current == '\t') {
        if (current == '\n') {
          begin = end;
          ++end;
          eventHandler.terminal("LF", begin, end, currentLine);
          currentLine++;
        } else if (current == '\t') {
          ++end;
        } else if (current == '\r') {
          begin = end;
          ++end;
          if (input[end] != '\n') {
            throwException(end, "Whitespaces", "CR endings are banned");
            ++end;
          } else {
            ++end;
            eventHandler.terminal("CRLF", begin, end, currentLine);
            currentLine++;
          }
        } else if (current == ' ') {
          ++end;
        }

        if (end == size) {
          end = size;
          break;
        } else {
          current = input[end];
        }
      }

      begin = end;
      eventHandler.endNonterminal("Whitespaces", end);
    }

    private void parseTk_String() {
      bool isOk = true;
      if (input[end] == '"') {
        ++end;
        while (end < size && input[end] != '"') {
          if (input[end] == '\\') {
            if (end + 1 >= size || (input[end + 1] != '\\' && input[end + 1] != '\"')) {
              throwException(end, "String", "Unexpected symbol");
              ++end;
              isOk = false;
            } else {
              end += 2;
            }
          } else {
            ++end;
          }
        }

        ++end;
      } else {
        parse_recString();
      }

      if (isOk) {
        eventHandler.terminal("String", begin, end, currentLine);
      }

      begin = end;
    }

    private void parse_recString() {
      ++end;
      while (end < size && input[end] != '»') {
        if (input[end] == '«') {
          parse_recString();
        } else {
          ++end;
        }
      }

      ++end;
    }

    private void parse_Num() {
      bool signed = false;
      bool isOk = true;
      if (end + 1 < size && input[end] == '0' && input[end + 1] == 'x') {
        end += 2;
        if (end < size && ((Char.IsDigit(input[end]) || (input[end] <= 'f' && input[end] >= 'a') || (input[end] <= 'F' && input[end] >= 'A')))) {
          while (end < size && ((Char.IsDigit(input[end]) || (input[end] <= 'f' && input[end] >= 'a') || (input[end] <= 'F' && input[end] >= 'A')))) {
            ++end;
          }
        } else {
          throwException(end, "Number", "There is a hex number without a digits after \'0x\' ");
          isOk = false;
        }
      } else if (input[end] == '+' || input[end] == '-') {
        signed = true;
        ++end;
        if (input[end] == '0') {
          ++end;
        } else {
          while (end < size && Char.IsDigit(input[end])) {
            ++end;
          }
        }
      } else {
        if (input[end] == '0') {
          ++end;
        } else {
          while (end < size && Char.IsDigit(input[end])) {
            ++end;
          }
        }
      }

      if (isOk && end < size && (input[end] == '.' || input[end] == 'e' || input[end] == 'E')) {
        parseTk_Real();
      } else if (isOk && end < size && (input[end] == 'n' || input[end] == 'i')) {
        parseTk_Number(signed);
      } else if (isOk) {
        eventHandler.terminal("Number", begin, end, currentLine);
      }

      begin = end;
    }

    private void parseTk_Number(bool signed) {
      bool isOk = true;
      if (signed) {
        if (end < size && input[end] == 'n') {
          throwException(begin, "Number", "n-numbers can't have sign");
          isOk = false;
        } else if (end < size && input[end] == 'i') {
          ++end;
          if (end < size) {
            if (input[end] == '8' || input[end] == 'x') {
              ++end;
            } else if (end + 1 < size) {
              if ((input[end] == '3' && input[end + 1] == '2') || (input[end] == '1' && input[end + 1] == '6') || (input[end] == '6' && input[end + 1] == '4')) {
                end += 2;
              } else {
                throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i");
                isOk = false;
              }
            } else {
              throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i");
              isOk = false;
            }
          } else {
            throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i");
            isOk = false;
          }
        }
      } else if (end < size && (input[end] == 'i' || input[end] == 'n')) {
        ++end;
        if (end < size) {
          if (input[end] == '8' || input[end] == 'x') {
            ++end;
          } else if (end + 1 < size) {
            if ((input[end] == '3' && input[end + 1] == '2') || (input[end] == '1' && input[end + 1] == '6') || (input[end] == '6' && input[end + 1] == '4')) {
              end += 2;
            } else {
              throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i or n");
              isOk = false;
            }
          } else {
            throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i or n");
            isOk = false;
          }
        } else {
          throwException(end, "Number", "There is nothing like x, 8, 16, 32 or 64 after i or n");
          isOk = false;
        }
      }

      if (isOk) {
        eventHandler.terminal("Number", begin, end, currentLine);
      }

      begin = end;
    }

    private void parseTk_Real() {
      bool isOk = true;
      if (input[end] == '.') {
        ++end;
        if (end < size && Char.IsDigit(input[end])) {
          while (end < size && input[end] != 'e' && input[end] != 'E' && Char.IsDigit(input[end])) {
            ++end;
          }
        } else {
          throwException(end, "Real", "There must be a digit after point");
          isOk = false;
        }

        if (end < size && (input[end] == 'e' || input[end] == 'E')) {
          ++end;
          if (end < size && (input[end] == '+' || input[end] == '-')) {
            ++end;
            if (end < size && input[end] == '0') {
              ++end;
            } else if (end < size && Char.IsDigit(input[end])) {
              while (end < size && Char.IsDigit(input[end])) {
                ++end;
              }
            } else {
              throwException(end, "Real", "There must be a digit after \'E\'");
              isOk = false;
            }
          } else if (end < size && input[end] == '0') {
            ++end;
          } else if (end < size && Char.IsDigit(input[end])) {
            while (end < size && Char.IsDigit(input[end])) {
              ++end;
            }
          } else {
            throwException(end, "Real", "There must be a digit after \'E\'");
            isOk = false;
          }
        }
      } else if (input[end] == 'e' || input[end] == 'E') {
        ++end;
        if (end < size && (input[end] == '+' || input[end] == '-')) {
          ++end;
          if (end < size && input[end] == '0') {
            ++end;
          } else if (end < size && Char.IsDigit(input[end])) {
            while (end < size && Char.IsDigit(input[end])) {
              ++end;
            }
          } else {
            throwException(end, "Real", "There must be a digit after \'E\'");
            isOk = false;
          }
        } else if (end < size && input[end] == '0') {
          ++end;
        } else if (end < size && Char.IsDigit(input[end])) {
          while (end < size && Char.IsDigit(input[end])) {
            ++end;
          }
        } else {
          throwException(end, "Real", "There must be a digit after \'E\'");
          isOk = false;
        }
      } else {
        throwException(end, "Real", "There is an invalid character in the token");
        isOk = false;
      }

      if (end < size && input[end] == 'r') {
        ++end;
        if (end + 1 < size && ((input[end] == '3' && input[end + 1] == '2') || (input[end] == '6' && input[end + 1] == '4'))) {
          end += 2;
        } else {
          throwException(end, "Real", "There is nothing like 32 or 64 after r");
          isOk = false;
        }
      }

      if (isOk) {
        eventHandler.terminal("Real", begin, end, currentLine);
      }

      begin = end;
    }

    private void parseTk_Comment() {
      ++end;
      int lastNonWS = end;
      while (end < size && input[end] != '\n' && input[end] != '\r') {
        if (input[end] == ' ' || input[end] == '\t') {
          ++end;
        } else {
          ++end;
          lastNonWS = end;
        }
      }

      eventHandler.terminal("Comment", begin, lastNonWS, currentLine);
      begin = end;
    }

    private void parse_Name() {
      isBuiltin = false;
      switch (input[end]) {
        case '@':
        isBuiltin = true;
        ++end;
        break;
        case '!':
        isBuiltin = true;
        ++end;
        break;
        case '.':
        ++end;
        while (end < size && input[end] == '.') {
          ++end;
        }

        break;
        default:
        parseTk_MemberName();
        break;
      }
    }

    private void parseTk_MemberName() {
      string nameWord = "";
      if (end < size && (input[end] == '+' || input[end] == '-')) {
        nameWord += input[end];
        ++end;
        if (end < size && (checkLetter(input[end]) || input[end] == '+' || input[end] == '-')) {
          nameWord += input[end];
          ++end;
          while (end < size && (checkLetter(input[end]) || Char.IsDigit(input[end]) || input[end] == '+' || input[end] == '-')) {
            nameWord += input[end];
            ++end;
          }

          if (Constants.MplBuiltins.Contains(nameWord)) {
            isBuiltin = true;
          }
        } else {
          if (Constants.MplBuiltins.Contains(nameWord)) {
            isBuiltin = true;
          }

          return;
        }
      } else if (end < size && checkLetter(input[end])) {
        nameWord += input[end];
        ++end;
        while (end < size && (checkLetter(input[end]) || Char.IsDigit(input[end]) || input[end] == '+' || input[end] == '-')) {
          nameWord += input[end];
          ++end;
        }

        if (Constants.MplBuiltins.Contains(nameWord)) {
          isBuiltin = true;
        }
      } else {
        throwException(end, "MemberName", "It must be MemberName here, but it's empty");
      }
    }

    private void parse_NameExpressionOrLableOrLableReset() {
      parse_Name();
      if (end + 1 < size && input[end] == ':' && input[end + 1] == '!') {
        parse_LabelReset();
      } else if (end < size && input[end] == ':') {
        parse_Label();
      } else {
        eventHandler.startNonterminal("NameExpression", begin, currentLine);
        if (isBuiltin) {
          eventHandler.terminal("Builtin", begin, end, currentLine);
        } else {
          eventHandler.terminal("Name", begin, end, currentLine);
        }

        eventHandler.endNonterminal("NameExpression", end);
        begin = end;
      }

      isBuiltin = false;
    }

    private bool checkName(char c) {
      return (checkLetter(c) || c == '.');
    }

    private bool checkLetter(char c) {
      return (c != '+' && !Char.IsDigit(c) && c != ' ' && c != '.' && c != ';' && c != ':' && c != ',' && c != '!' && c != '@' && c != '{' && c != '}' && c != '(' && c != ')' && c != 0x002D && c != 0x0022 && c != 0x0023 && c != 0x005B && c != 0x005D && c != 0x0009 && c != 0x000A && c != 0x000D);
    }

    private void findNextWhitespace() {
      while (end < size && input[end] != ' ' && input[end] != '\n' && input[end] != '\r') {
        ++end;
      }
    }

    private void throwException(int position, int line, string token, string message) {
      eventHandler.pushError(position, line, position - (input.Substring(0, position)).LastIndexOf('\n'), token, message);
    }

    private void throwException(int position, string token, string message) {
      eventHandler.pushError(position, currentLine, position - (input.Substring(0, position)).LastIndexOf('\n'), token, message);
    }

    private void getName(int begin , int end) {
      string name = input.Substring(begin, end - begin);
      eventHandler.getName(name);
    }

  }
}