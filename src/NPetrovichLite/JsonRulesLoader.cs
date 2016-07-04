﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NPetrovichLite
{
    internal class JsonRulesLoader
    {
        private const string RESOURCE_NAME = "NPetrovichLite.rules.json";
        private static readonly int MODIFIERS_COUNT = Enum.GetValues(typeof(Case)).Length - 1;

        internal static RulesContainer LoadEmbeddedResource()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(RESOURCE_NAME))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    JsonRulesLoader loader = new JsonRulesLoader(reader);
                    return loader.m_data;
                }
            }
        }

        private readonly RulesContainer m_data = new RulesContainer();

        private readonly JsonParser m_parser;

        private JsonRulesLoader(StreamReader reader)
        {
            m_parser = new JsonParser(reader);

            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ObjectStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ObjectEnd))
            {
                ParseRuleSet();
            }
        }

        private void ParseRuleSet()
        {
            string rulePartName = m_parser.GetNextPropertyName();

            if (rulePartName == "gender")
            {
                ParseGenderRulesContainer();
            }
            else
            {
                NamePart rulePart = ParseNamePart(rulePartName);
                PartRules rules = ParsePartRulesList();
                m_data[rulePart] = rules;
            }
        }

        private PartRules ParsePartRulesList()
        {
            PartRules result = new PartRules();
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ObjectStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ObjectEnd))
            {
                bool ruleGroupIsSuffix;
                string ruleGroupName = m_parser.GetNextPropertyName();
                switch (ruleGroupName)
                {
                case "exceptions":
                    ruleGroupIsSuffix = false;
                    break;
                case "suffixes":
                    ruleGroupIsSuffix = true;
                    break;
                default:
                    throw new ParseException("Unexpected rule group '" + ruleGroupName + "'");
                }

                m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayStart);
                while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ArrayEnd))
                {
                    IRule rule = ParseRule(ruleGroupIsSuffix);
                    result.Add(rule);
                }
            }
            return result;
        }

        private IRule ParseRule(bool ruleGroupIsSuffix)
        {
            Gender? gender = null;
            string[] test = null;
            IModifier[] modifiers = null;
            Tags tags = Tags.None;

            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ObjectStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ObjectEnd))
            {
                string propertyName = m_parser.GetNextPropertyName();
                switch (propertyName)
                {
                case "gender":
                    gender = ParseGender(m_parser.GetNextStringValue());
                    break;
                case "test":
                    test = ParseTestStrings();
                    break;
                case "mods":
                    modifiers = ParseModifiers();
                    break;
                case "tags":
                    tags = ParseTags();
                    break;
                }
            }
            if (gender == null)
            {
                throw new ParseException("Failed to parse rule, no gender specified");
            }
            if (test == null)
            {
                throw new ParseException("Failed to parse rule, no test strings specified");
            }
            if (modifiers == null)
            {
                throw new ParseException("Failed to parse rule, no modifiers specified");
            }
            
            if (ruleGroupIsSuffix)
            {
                return new SufixRule(gender.Value, tags, test, modifiers);
            }
            else
            {
                return new ExceptionRule(gender.Value, tags, test, modifiers);
            }
        }

        private Tags ParseTags()
        {
            Tags tags = Tags.None;
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ArrayEnd))
            {
                tags |= ParseTagsValue(m_parser.GetNextStringValue());
            }
            return tags;
        }


        private IModifier[] ParseModifiers()
        {
            IModifier[] result = new IModifier[MODIFIERS_COUNT];
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayStart);
            for (int i = 0; i < MODIFIERS_COUNT; ++i)
            {
                result[i] = ParseModifier(m_parser.GetNextStringValue());
            }
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayEnd);
            return result;
        }

        private IModifier ParseModifier(string value)
        {
            if (value == ".")
            {
                return IdentityModifier.Instance;
            }
            return new SuffixModifier(value);
        }

        private string[] ParseTestStrings()
        {
            List<string> result = new List<string>();

            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ArrayEnd))
            {
                result.Add(m_parser.GetNextStringValue());
            }

            return result.ToArray();
        }

        private void ParseGenderRulesContainer()
        {
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ObjectStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ObjectEnd))
            {
                string rulePartName = m_parser.GetNextPropertyName();
                NamePart rulePart = ParseNamePart(rulePartName);
                ParseGenderRules(m_data.genderRules[rulePart]);
            }
        }

        private void ParseGenderRules(GenderRules rules)
        {
            m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ObjectStart);
            while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ObjectEnd))
            {
                string genderStr = m_parser.GetNextPropertyName();
                Gender gender = ParseGender(genderStr);
                m_parser.AssertNextTokenTypeAndConsume(JsonParser.TokenType.ArrayStart);
                while (!m_parser.CheckNextTokenTypeAndConsumeIfTrue(JsonParser.TokenType.ArrayEnd))
                {
                    string suffix = m_parser.GetNextStringValue();
                    if (rules.ContainsKey(suffix))
                    {
                        throw new ParseException(String.Format("Duplicate suffix '{0}' for gender {1}", suffix, gender));
                    }
                    rules.Add(suffix, gender);
                }
            }
        }

        private static Tags ParseTagsValue(string value)
        {
            switch (value)
            {
            case "first_word":
                return Tags.FirstWord;
            default:
                throw new ParseException("Unknown tag : '" + value + "'");
            }
        }

        private static NamePart ParseNamePart(string value)
        {
            switch (value)
            {
            case "lastname":
                return NamePart.LastName;
            case "firstname":
                return NamePart.FirstName;
            case "middlename":
                return NamePart.MiddleName;
            default:
                throw new ParseException("Unknown name part : '" + value + "'");
            }
        }

        private Gender ParseGender(string value)
        {
            switch (value)
            {
            case "female":
                return Gender.Female;
            case "male":
                return Gender.Male;
            case "androgynous":
                return Gender.Androgynous;
            default:
                throw new ParseException("Unknown gender: '" + value + "'");
            }
        }
    }
}
