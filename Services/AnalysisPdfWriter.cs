using CareerSync.Models;
using System.Globalization;
using System.Text;

namespace CareerSync.Services
{
    public static class AnalysisPdfWriter
    {
        public static byte[] Build(MatchResult result, CV cv, JobDescription job, string graduate)
        {
            var lines = new List<string>
        {
            "CareerSyncAI | Career Readiness Report",
            "Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture),
            "Graduate: " + graduate,
            "CV: " + cv.FileName,
            "Job: " + job.Title + " | " + job.Company,
            "Analysis date: " + result.GeneratedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "",
            "OVERALL COMPATIBILITY: " + result.OverallScore + "%",
            "Technical skills: " + result.TechnicalSkillsScore + "%",
            "Experience: " + result.ExperienceScore + "%",
            "Qualifications: " + result.QualificationsScore + "%",
            "",
            "MATCHED SKILLS",
        };
            lines.AddRange(result.MatchedSkills.Count == 0 ? new[] { "No matched skills identified." } : result.MatchedSkills.Select(x => "- " + x));
            lines.Add(""); lines.Add("SKILLS TO IMPROVE");
            lines.AddRange(result.GapSkills.Count == 0 ? new[] { "No missing skills identified." } : result.GapSkills.Select(x => "- " + x));
            lines.Add("");
            lines.Add("This rule-based score is a career guidance estimate, not a hiring decision.");
            var wrapped = new List<string>();
            foreach (var line in lines)
            {
                var clean = new string(line.Select(c => c is >= ' ' and <= '~' ? c : ' ').ToArray());
                while (clean.Length > 92) { wrapped.Add(clean[..92]); clean = clean[92..]; }
                wrapped.Add(clean);
            }
            var pages = wrapped.Chunk(47).ToArray();
            var obj = new List<string>();
            int Add(string value) { obj.Add(value); return obj.Count; }
            int catalog = Add(""); int pageTree = Add("");
            int font = Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            var pageIds = new List<int>();
            for (int i = 0; i < pages.Length; i++)
            {
                var text = new StringBuilder("BT /F1 11 Tf 48 790 Td 16 TL ");
                foreach (var line in pages[i])
                {
                    var escaped = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                    text.Append('(').Append(escaped).Append(") Tj T* ");
                }
                text.Append("ET");
                var data = text.ToString();
                var content = Add("<< /Length " + Encoding.ASCII.GetByteCount(data) + " >>\nstream\n" + data + "\nendstream");
                int page = Add("<< /Type /Page /Parent " + pageTree + " 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 " + font + " 0 R >> >> /Contents " + content + " 0 R >>");
                pageIds.Add(page);
            }
            obj[catalog - 1] = "<< /Type /Catalog /Pages " + pageTree + " 0 R >>";
            obj[pageTree - 1] = "<< /Type /Pages /Kids [" + string.Join(" ", pageIds.Select(i => i + " 0 R")) + "] /Count " + pageIds.Count + " >>";
            using var stream = new MemoryStream();
            void Put(string s) { var bytes = Encoding.ASCII.GetBytes(s); stream.Write(bytes, 0, bytes.Length); }
            Put("%PDF-1.4\n");
            var offsets = new List<long> { 0 };
            for (int i = 0; i < obj.Count; i++) { offsets.Add(stream.Position); Put((i + 1) + " 0 obj\n" + obj[i] + "\nendobj\n"); }
            long xref = stream.Position;
            Put("xref\n0 " + (obj.Count + 1) + "\n0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++) Put(offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            Put("trailer\n<< /Size " + (obj.Count + 1) + " /Root " + catalog + " 0 R >>\nstartxref\n" + xref + "\n%%EOF");
            return stream.ToArray();
        }
    }

}

