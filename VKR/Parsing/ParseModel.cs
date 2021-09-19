using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AngleSharp.Html.Parser;
using System.Threading;
using System.Web.Mvc;
using VKR.Models;
using System.Net;
using System.IO;

namespace VKR.Parsing
{
    public class ParseModel
    {
        public void Start(List<string> groupScheduleList)
        {
            ScheduleDBEntities dbContext = new ScheduleDBEntities();
            //очищение изменяемых данных для нового парсинга
            if (dbContext.Schedules != null)
                dbContext.Schedules.RemoveRange(dbContext.Schedules);
            dbContext.SaveChanges();
            //if (dbContext.Lecturers != null)
            //    dbContext.Lecturers.RemoveRange(dbContext.Lecturers);
            //if (dbContext.Groups != null)
            //    dbContext.Groups.RemoveRange(dbContext.Groups);
            //if (dbContext.Disciplines != null)
            //    dbContext.Disciplines.RemoveRange(dbContext.Disciplines);
            //dbContext.SaveChanges();

            //определение факультета
            int faculty = 0;

            foreach (var groupSchedule in groupScheduleList)
                GetScheduleInfo(groupSchedule, ref faculty, dbContext);
        }


        public void GetScheduleInfo(string scheduleLink, ref int faculty, ScheduleDBEntities dbContext)
        {
            var content = GetRequest(scheduleLink);
            HtmlParser parser = new HtmlParser();
            var htmlDocument = parser.ParseDocument(content);
            Thread.Sleep(1000);

            //считываем данные группы
            var numberGroup = scheduleLink.Replace("https://www.smtu.ru/ru/viewschedule/", string.Empty).Replace("/", string.Empty);
            var dayOfWeekList = htmlDocument?.QuerySelectorAll("table thead")?.Skip(1)?.Select(x => x?.TextContent?.Trim())?.ToList() ?? new List<string>();
            var daySchedule = htmlDocument?.QuerySelectorAll("table tbody")?.Skip(1)?.ToList() ?? new List<AngleSharp.Dom.IElement>();
            //определяем номер факультета
            if (faculty != int.Parse(numberGroup.Remove(numberGroup.Length - 3)))
            {
                faculty = int.Parse(numberGroup.Remove(numberGroup.Length - 3));
                if (faculty == 1)
                    if (int.Parse(numberGroup.Substring(2)) == 15)//если код напрааления кончается на 15 - то иностранцы
                        faculty = 11;
                if (faculty == 6)
                    faculty = 5;
            }

            //
            //добавление данных в таблицы
            //
            var groupInDB = dbContext.Groups.FirstOrDefault(g => g.Group_number == numberGroup);
            if (groupInDB == null)
            {
                Groups gr = new Groups
                {
                    Group_number = numberGroup,
                    FacultyId = faculty
                };
                dbContext.Groups.Add(gr);
                dbContext.SaveChanges();
            }
            for (int i = 0; i < dayOfWeekList.Count; i++)
            {
                var subjectList = daySchedule[i]?.QuerySelectorAll("tr")?.ToList() ?? new List<AngleSharp.Dom.IElement>();
                foreach (var subjectElement in subjectList)
                {
                    var typeWeek = subjectElement.QuerySelector("td:first-child .s_small")?.TextContent?.Trim() ?? string.Empty;
                    var time = typeWeek == string.Empty ? subjectElement.QuerySelector("td:first-child")?.TextContent?.Trim() ?? string.Empty
                        : subjectElement.QuerySelector("td:first-child")?.TextContent?.Replace(typeWeek, string.Empty)?.Trim() ?? string.Empty;
                    var audience = subjectElement.QuerySelector("td:nth-child(2)")?.TextContent?.Trim() ?? string.Empty;
                    var typeSubject = subjectElement.QuerySelector("td:nth-child(3) .s_small")?.TextContent?.Trim() ?? string.Empty;
                    var subjectName = subjectElement.QuerySelector("td:nth-child(3)")?.TextContent?.Replace(typeSubject, string.Empty)?.Trim() ?? string.Empty;
                    var teacher = subjectElement.QuerySelector("td:nth-child(4)")?.TextContent?.Trim() ?? string.Empty;
                    //добавление данных в таблицы
                    AddToDatabase(dbContext, dayOfWeekList[i], time, typeWeek, numberGroup, teacher, audience, subjectName);
                }
            }
        }

        private void AddToDatabase(ScheduleDBEntities dbContext, string dayOfWeekList, string time, string typeWeek, string numberGroup, string teacher, string audience, string subjectName)
        {
            var lecturerInDB = dbContext.Lecturers.FirstOrDefault(l => l.LecturerName == teacher);
            if (lecturerInDB == null)
            {
                Lecturers lr = new Lecturers
                {
                    LecturerName = teacher
                };
                dbContext.Lecturers.Add(lr);
                dbContext.SaveChanges();
            }

            var auditoriasInDB = dbContext.Auditorias.FirstOrDefault(l => l.AuditoriaName == audience);
            if (auditoriasInDB == null)
            {
                string building;
                switch (audience[0])
                {
                    case 'У':
                        building = "Корпус У";
                        break;
                    case 'А':
                        building = "Корпус У";
                        break;
                    case 'Б':
                        building = "Корпус У";
                        break;
                    case 'Г':
                        building = "Корпус У";
                        break;
                    default:
                        building = null;
                        break;
                }

                Auditorias auds = new Auditorias
                {
                    AuditoriaName = audience,
                    AuditoriaBuilding = building,
                    IsOnMap = false
                };
                dbContext.Auditorias.Add(auds);
                dbContext.SaveChanges();
            }

            var disciplineInDB = dbContext.Disciplines.FirstOrDefault(l => l.DisciplineName == subjectName);
            if (disciplineInDB == null)
            {
                Disciplines lr = new Disciplines
                {
                    DisciplineName = subjectName
                };
                dbContext.Disciplines.Add(lr);
                dbContext.SaveChanges();
            }

            //Поиск значений элемента таблицы Schedules
            SearchIdInDB(out int gr, out int? ti, out int? au, out int? di, out int? le, out int? ty, out int? da, 
                dayOfWeekList, time, typeWeek, numberGroup, teacher, audience, subjectName);

            //добавление записи о расписании
            Schedules sh = new Schedules
            {
                GroupId = gr,
                DayOfWeekId = da,
                TimeId = ti,
                TypeOfWeekId = ty,
                AuditoriaId = au,
                DisciplineId = di,
                LecturerId = le
            };
            dbContext.Schedules.Add(sh);
            dbContext.SaveChanges();
        }

        private void SearchIdInDB(out int gr, out int? ti, out int? au, out int? di, out int? le, out int? ty, out int? da,
            string dayOfWeekList, string time, string typeWeek, string numberGroup, string teacher, string audience, string subjectName)
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                var element1 = dbContext.Groups.FirstOrDefault(g => g.Group_number == numberGroup);
                gr = element1.GroupId;

                var element2 = dbContext.Times.FirstOrDefault(g => g.TimePeriod == time);
                ti = null;
                if (element2 != null)
                    ti = dbContext.Times.FirstOrDefault(g => g.TimePeriod == time).TimeId;

                var element3 = dbContext.Auditorias.FirstOrDefault(g => g.AuditoriaName == audience);
                au = null;
                if (element3 != null)
                    au = element3.AuditoriaId;

                var element4 = dbContext.Disciplines.FirstOrDefault(g => g.DisciplineName == subjectName);
                di = null;
                if (element4 != null)
                    di = element4.DisciplineId;

                var element5 = dbContext.Lecturers.FirstOrDefault(g => g.LecturerName == teacher);
                le = null;
                if (element5 != null)
                    le = element5.LecturerId;

                var element6 = dbContext.TypesOfWeek.FirstOrDefault(g => g.TypeName == typeWeek);
                ty = null;
                if (element6 != null)
                    ty = element6.TypeOfWeekId;

                var element7 = dbContext.DaysOfWeek.FirstOrDefault(g => g.DayOfWeekName == dayOfWeekList);
                da = null;
                if (element7 != null)
                    da = element7.DayOfWeekId;
            }
        }

        public static string GetRequest(string url)
        {
            var result = string.Empty;
            //тут
            WebRequest request = WebRequest.Create(url);
            //тут
            WebResponse response = request.GetResponse();
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            response.Close();
            return result;
        }

    }

    
}