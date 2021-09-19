using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace VKR.Models 
{
    public class Analyst
    {
        enum PatientType { Группа, Преподаватель}

        //входные данные
        PatientType typePatient { get; set; }
        public string PatientName { get; set; }
        public DateTime DateStartAnalize { get; set; }
        public DateTime DateSymptom { get; set; }
        public DateTime DateStopInfect { get; set; }
        public DateTime DateStopAnalize { get; set; }
        
        //для анализа
        public List<InfectedAuditoria> InfectedAuditorias { get; set; }
        //анализ формирует список инфицированных аудиторий.
        public List<Groups> InfectedGroups { get; set; }
        public List<Lecturers> InfectedLecturers { get; set; }

        //результат
        public List<AuditoriaOnMap> AuditoriasOnMap { get; set; }


        public Analyst(string radioTypePatient, string Group, string Lecturer, DateTime DateSymptom, DateTime DateStopInfect, DateTime DateStopAnalize)
        {
            if (radioTypePatient == PatientType.Группа.ToString())
            {
                this.typePatient = PatientType.Группа;
                this.PatientName = Group;
            }
            else if (radioTypePatient == PatientType.Преподаватель.ToString())
            {
                this.typePatient = PatientType.Преподаватель;
                this.PatientName = Lecturer;
            }
            this.DateSymptom = DateSymptom;
            this.DateStopInfect = DateStopInfect;
            this.DateStopAnalize = DateStopAnalize;
            DateStartAnalize = this.DateSymptom.AddDays(-2);
            InfectedAuditorias = new List<InfectedAuditoria>();
            InfectedGroups = new List<Groups>();
            InfectedLecturers = new List<Lecturers>();
            AuditoriasOnMap = new List<AuditoriaOnMap>();
            
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                var audsOnMap = dbContext.Auditorias.Where(x => x.IsOnMap == true).ToList();

                var audsOnMapInSchedule = (from au in dbContext.Auditorias
                          where dbContext.Schedules.Select(x => x.AuditoriaId).Contains(au.AuditoriaId)
                          && au.IsOnMap == true
                          select au).ToList();
                //проверка на наличие данных в бд
                if (audsOnMap != null)
                    foreach (Auditorias a in audsOnMap)
                    {
                        AuditoriaOnMap aon;
                        if (audsOnMapInSchedule.FirstOrDefault(x => x.AuditoriaId == a.AuditoriaId) != null)
                            aon = new AuditoriaOnMap(a.AuditoriaName, AuditoriaOnMap.status.NotInfected);
                        else
                            aon = new AuditoriaOnMap(a.AuditoriaName, AuditoriaOnMap.status.NotInSchedule);
                        AuditoriasOnMap.Add(aon);
                    }
            }
        }

        public string MakeResearch()
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                if (HasBadStartData() != null)
                    return HasBadStartData();

                Auditorias audDistant1 = dbContext.Auditorias.FirstOrDefault(a => a.AuditoriaName == "ЦДО Дистанционно");
                Auditorias audDistant2 = dbContext.Auditorias.FirstOrDefault(a => a.AuditoriaName == "Дистанционно");
                Auditorias audForeignLang = dbContext.Auditorias.FirstOrDefault(a => a.AuditoriaName == "У каф.ИЯ");
                Auditorias audMilitary = dbContext.Auditorias.FirstOrDefault(a => a.AuditoriaName == "У каф.ФВ");
                Lecturers noLecturer = dbContext.Lecturers.FirstOrDefault(l => l.LecturerName.Length < 1);
                List<InfectedAuditoria> stillInfectedAuditorias = new List<InfectedAuditoria>();

                //начинаем ежедневный мониторинг распространения и фиксирование информации
                for (DateTime analizeDay = DateStartAnalize; analizeDay <= DateStopAnalize; analizeDay = analizeDay.AddDays(1))
                {
                    //вышел ли нулевой пациент на карантин
                    if (analizeDay >= DateStopInfect)
                        ZeroPatientStopsInfect();

                    //информация по текущему дню
                    int codeDayOfWeek = (int)analizeDay.DayOfWeek;
                    string typeOfWeekName = GetTypeOfWeek(analizeDay);
                    int codeTypeOfWeek = dbContext.TypesOfWeek.First(t => t.TypeName == typeOfWeekName).TypeOfWeekId;

                    if (codeDayOfWeek != 0)
                    {
                        //идем последовательно по парам в порядке расписания
                        for (int codeTime = 1; codeTime <= 8; codeTime++)
                        {
                            //ищем какие аудитории заражены от групп
                            CheckNewInfectByGroup(noLecturer, audDistant1, audDistant2, audForeignLang, audMilitary, codeDayOfWeek, codeTime, codeTypeOfWeek, analizeDay);
                            //ищем какие аудитории заражены от преподавателей
                            CheckNewInfectByLecturer(noLecturer, audDistant1, audDistant2, audForeignLang, audMilitary, codeDayOfWeek, codeTime, codeTypeOfWeek, analizeDay);
                            //ищем, кто заразился от зараженной аудитории
                            CheckNewInfectByAuditoria(ref stillInfectedAuditorias, noLecturer, audDistant1, audDistant2, audForeignLang, audMilitary, codeDayOfWeek, codeTime, codeTypeOfWeek, analizeDay);
                        }
                    }
                }

                //формируем данные по карте на основе данных об инфицированных аудиториях
                foreach (AuditoriaOnMap aon in AuditoriasOnMap)
                {
                    Auditorias audDB = dbContext.Auditorias.FirstOrDefault(x => x.AuditoriaName == aon.Name);
                    if (audDB == null)
                        return $"Не найдена аудитория с именем {aon.Name}.";
                    InfectedAuditoria ia = InfectedAuditorias.FirstOrDefault(x => x.AuditoriaId == audDB.AuditoriaId);
                    if (ia != null)
                        aon.AddInfectData(ia);
                }
            }
            var ll = InfectedLecturers.OrderBy(x => x.LecturerName);
            var gg = InfectedGroups.OrderBy(x => x.Group_number);
            return "Анализ успешно завершен";
        }

        #region Функции проверок данных для грамотного проведения анализа
        string HasBadStartData()
        {
            if (PatientName == null || DateStartAnalize == null || DateStopAnalize == null || DateStopInfect == null || DateSymptom == null)
                return "Недостаточно данных для анализа. Введите все стартовые параметры и попробуйте вновь.";

            if (AuditoriasOnMap == null)
                return "В БД нет данных о аудиториях на карте (IsOnMap).";

            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                //добавляем стартовые данные для начала анализа распространения вируса
                if (typePatient == PatientType.Группа)
                {
                    Groups ig = dbContext.Groups.FirstOrDefault(g => g.Group_number == PatientName);
                    if (ig == null)
                        return $"В БД нет данных о группе: №{PatientName}.";
                    InfectedGroups.Add(ig);
                }
                else if (typePatient == PatientType.Преподаватель)
                {
                    Lecturers lr = dbContext.Lecturers.FirstOrDefault(l => l.LecturerName == PatientName);
                    if (lr == null)
                        return $"В БД нет данных о лекторе: {PatientName}.";
                    InfectedLecturers.Add(lr);
                }
            }
            return null;
        }

        void ZeroPatientStopsInfect()
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                if (typePatient == PatientType.Группа)
                {
                    Groups g = dbContext.Groups.First(a => a.Group_number == PatientName);
                    InfectedGroups.Remove(g);
                }
                if (typePatient == PatientType.Преподаватель)
                {
                    Lecturers l = dbContext.Lecturers.First(a => a.LecturerName == PatientName);
                    InfectedLecturers.Remove(l);
                }
            }
        }
        #endregion

        #region Методы внутри алгоритма Анализа

        void CheckNewInfectByGroup(Lecturers noLecturer, Auditorias audDistant1, Auditorias audDistant2, Auditorias audForeignLang, Auditorias audMilitary, int codeDayOfWeek, int codeTime, int codeTypeOfWeek, DateTime analizeDay )
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                //ищем какие аудитории заражены от группы
                foreach (Groups g in InfectedGroups)
                {
                    var classes = dbContext.Schedules.Where(
                        s =>
                        s.AuditoriaId != audDistant1.AuditoriaId && s.AuditoriaId != audDistant2.AuditoriaId && s.AuditoriaId != audForeignLang.AuditoriaId && s.AuditoriaId != audMilitary.AuditoriaId && 
                        s.GroupId == g.GroupId &&
                        s.DayOfWeekId == codeDayOfWeek &&
                        s.TimeId == codeTime &&
                        s.LecturerId != noLecturer.LecturerId &&
                        (s.TypeOfWeekId == null || s.TypeOfWeekId == codeTypeOfWeek)
                        ).ToArray();
                    //для каждой записи в расписании у зараженной группы - добавляем данные о новой инфицированной аудитории
                    foreach (Schedules cl in classes)
                        UpdateNewInfectedAuditoria(cl, analizeDay, g.Group_number);
                }
            }       
        }

        void CheckNewInfectByLecturer(Lecturers noLecturer, Auditorias audDistant1, Auditorias audDistant2, Auditorias audForeignLang, Auditorias audMilitary, int codeDayOfWeek, int codeTime, int codeTypeOfWeek, DateTime analizeDay)
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                //ищем какие аудитории заражены от преподавателя
                foreach (Lecturers l in InfectedLecturers)
                {
                    var classes = dbContext.Schedules.Where(
                        s =>
                        s.AuditoriaId != audDistant1.AuditoriaId && s.AuditoriaId != audDistant2.AuditoriaId && s.AuditoriaId != audForeignLang.AuditoriaId && s.AuditoriaId != audMilitary.AuditoriaId &&
                        s.LecturerId == l.LecturerId && s.LecturerId != noLecturer.LecturerId &&
                        s.DayOfWeekId == codeDayOfWeek &&
                        s.TimeId == codeTime &&
                        (s.TypeOfWeekId == null || s.TypeOfWeekId == codeTypeOfWeek)
                        ).ToArray();
                    //для каждой записи в расписании у зараженной группы - обновляем данные о инфицированной аудитории
                    foreach (Schedules cl in classes)
                        UpdateNewInfectedAuditoria(cl, analizeDay, l.LecturerName);
                }
            }
        }

        void CheckNewInfectByAuditoria(ref List<InfectedAuditoria> stillInfectedAuditorias, Lecturers noLecturer, Auditorias audDistant1, Auditorias audDistant2, Auditorias audForeignLang, Auditorias audMilitary, int codeDayOfWeek, int codeTime, int codeTypeOfWeek, DateTime analizeDay)
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                // проверяем не прошло ли 4 дня без инфицирования у зараженных аудиторий - меняем поле
                foreach (InfectedAuditoria ia in InfectedAuditorias)
                {
                    ia.CheckIsStillInfected(analizeDay);
                    stillInfectedAuditorias = InfectedAuditorias.Where(x => x._isInfectedNow == true).ToList();
                }

                //ищем, кто заразился сидя в зараженной аудитории
                foreach (InfectedAuditoria ia in stillInfectedAuditorias)
                {
                    var classes = dbContext.Schedules.Where(
                        s =>
                        s.AuditoriaId == ia.AuditoriaId && s.AuditoriaId != audDistant1.AuditoriaId && s.AuditoriaId != audDistant2.AuditoriaId && s.AuditoriaId != audForeignLang.AuditoriaId && s.AuditoriaId != audMilitary.AuditoriaId &&
                        s.DayOfWeekId == codeDayOfWeek &&
                        s.LecturerId != noLecturer.LecturerId &&
                        s.TimeId == codeTime &&
                        (s.TypeOfWeekId == null || s.TypeOfWeekId == codeTypeOfWeek)
                        ).ToArray();

                    foreach (Schedules s in classes)
                    {
                        Lecturers lecturer = dbContext.Lecturers.First(l => l.LecturerId == s.LecturerId);
                        Groups group = dbContext.Groups.First(g => g.GroupId == s.GroupId);

                        if (InfectedLecturers.FirstOrDefault(x => x.LecturerId == lecturer.LecturerId)==null)
                            InfectedLecturers.Add(lecturer);
                        if (InfectedGroups.FirstOrDefault(x => x.GroupId == group.GroupId) == null)
                            InfectedGroups.Add(group);
                        ia.AddInfectedPeopleToia(lecturer, group);
                    }
                }
            }
        }


        void UpdateNewInfectedAuditoria(Schedules cl, DateTime analizeDay, string patientData)
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                Auditorias a = dbContext.Auditorias.First(x => x.AuditoriaId == cl.AuditoriaId);
                InfectedAuditoria iaSearched = InfectedAuditorias.FirstOrDefault(x => x.AuditoriaId == a.AuditoriaId); //FindInfectedAuditoria(a);
                if (iaSearched == null)
                {
                    InfectedAuditoria ia = new InfectedAuditoria(a.AuditoriaId, analizeDay, patientData);
                    InfectedAuditorias.Add(ia);
                }
                else
                {
                    iaSearched.EnterNewInfectionEvent(analizeDay, patientData);
                }
            }
        }
        #endregion


        string GetTypeOfWeek(DateTime day)
        {
            CultureInfo ci = new CultureInfo("ru-RU");
            Calendar cal = ci.Calendar;
            CalendarWeekRule cwr = ci.DateTimeFormat.CalendarWeekRule;
            System.DayOfWeek dow = ci.DateTimeFormat.FirstDayOfWeek;
            int weekNum = cal.GetWeekOfYear(day, cwr, dow);
            string type = weekNum % 2 == 0 ? "Верхняя неделя" : "Нижняя неделя";
            return type;
        }

    }

}