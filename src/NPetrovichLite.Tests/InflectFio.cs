﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NPetrovichLite.Tests
{
    [TestFixture]
    public sealed class InflectFio
    {
        private Petrovich petrovich;

        [SetUp]
        [OneTimeSetUp]
        public void Init()
        {
            petrovich = new Petrovich();
        }

        [Test]
        public void Test01()
        {
            Petrovich.FIO fio = new Petrovich.FIO() {
                lastName = "Иванов",
                firstName = "Иван",
                midName = "Иванович"
            };

            Petrovich.FIO result = petrovich.Inflect(fio, Case.Nominative);
            Assert.AreEqual(fio, result);

            result = petrovich.Inflect(fio, Case.Genitive);
            Assert.AreEqual("Иванова", result.lastName);
            Assert.AreEqual("Ивана", result.firstName);
            Assert.AreEqual("Ивановича", result.midName);
        }

        [Test]
        [TestCaseSource(typeof(TestDataFactory), nameof(TestDataFactory.LastNamesData))]
        [TestCaseSource(typeof(TestDataFactory), nameof(TestDataFactory.FirstNamesData))]
        [TestCaseSource(typeof(TestDataFactory), nameof(TestDataFactory.MidNamesData))]
        //[TestCaseSource(typeof(TestDataFactory), nameof(TestDataFactory.ReadSurnamesData))]
        public void TestMultipleBySinglePartInflection(string value, NamePart part, Gender gender, Case targetCase, string expected)
        {
            switch (part)
            {
            case NamePart.LastName:
                {
                    string result = petrovich.Inflect(new Petrovich.FIO() { lastName = value }, targetCase, gender).lastName;
                    Assert.AreEqual(expected, result, string.Format("Part: {0}, Gender: {1}, Case: {2}", part, gender, targetCase));
                }
                break;
            case NamePart.FirstName:
                {
                    string result = petrovich.Inflect(new Petrovich.FIO() { firstName = value }, targetCase, gender).firstName;
                    Assert.AreEqual(expected, result, string.Format("Part: {0}, Gender: {1}, Case: {2}", part, gender, targetCase));
                }
                break;
            case NamePart.MiddleName:
                {
                    string result = petrovich.Inflect(new Petrovich.FIO() { midName = value }, targetCase, gender).midName;
                    Assert.AreEqual(expected, result, string.Format("Part: {0}, Gender: {1}, Case: {2}", part, gender, targetCase));
                }
                break;
            }
            
        }

    }
}