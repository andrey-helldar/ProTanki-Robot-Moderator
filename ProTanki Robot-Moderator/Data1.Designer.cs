﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AIRUS_Bot_Moderator {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    public sealed partial class Data : global::System.Configuration.ApplicationSettingsBase {
        
        private static Data defaultInstance = ((Data)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Data())));
        
        public static Data Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string AccessToken {
            get {
                return ((string)(this["AccessToken"]));
            }
            set {
                this["AccessToken"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ai_rus")]
        public string Group {
            get {
                return ((string)(this["Group"]));
            }
            set {
                this["Group"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60")]
        public int Sleep {
            get {
                return ((int)(this["Sleep"]));
            }
            set {
                this["Sleep"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Deactivate {
            get {
                return ((bool)(this["Deactivate"]));
            }
            set {
                this["Deactivate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int Posts {
            get {
                return ((int)(this["Posts"]));
            }
            set {
                this["Posts"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string WordsDelete {
            get {
                return ((string)(this["WordsDelete"]));
            }
            set {
                this["WordsDelete"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60")]
        public int SleepDefault {
            get {
                return ((int)(this["SleepDefault"]));
            }
            set {
                this["SleepDefault"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int Length {
            get {
                return ((int)(this["Length"]));
            }
            set {
                this["Length"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Ban {
            get {
                return ((bool)(this["Ban"]));
            }
            set {
                this["Ban"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int BanPeriod {
            get {
                return ((int)(this["BanPeriod"]));
            }
            set {
                this["BanPeriod"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Delete {
            get {
                return ((bool)(this["Delete"]));
            }
            set {
                this["Delete"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("365")]
        public int DeleteDays {
            get {
                return ((int)(this["DeleteDays"]));
            }
            set {
                this["DeleteDays"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Likes {
            get {
                return ((bool)(this["Likes"]));
            }
            set {
                this["Likes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int LikesCount {
            get {
                return ((int)(this["LikesCount"]));
            }
            set {
                this["LikesCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int LikesOld {
            get {
                return ((int)(this["LikesOld"]));
            }
            set {
                this["LikesOld"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"{
	""words"" : [
		""акки"",
		""акка"",
		""хуй"",
		""ебать"",
		""пиздец"",
		""пизда"",
		""сука"",
		""иди на х**"",
		""х*йня"",
		""заебись"",
		""заебали"",
		""картошкакаквсегда"",
		""реально ли это?да"",
		""подробная информация на сайте"",
		""лохотрон"",
		""пиздо"",
		""Где купил"",
		""сделать приобретение"",
		""Всем советую"",
		""бабской"",
		""бабы"",
		""Продаётся"",
		""Продается"",
		""рукожопый"",
		""херня"",
		""worldoftanks."",
		""Vanomas"",
		""батлфилд"",
		""пиздит"",
		""пиздеть"",
		""спизди"",
		""нахуя"",
		""наёб"",
		""наеб"",
		""nvworld.ru"",
		""ёбывать"",
		""хуям"",
		""золото"",
		""Серб"",
		""Шторм"",
		""Кейсы"",
		""раздам"",
		""роздам"",
		""на стенке""
	]
}")]
        public string WordsDeleteDefault {
            get {
                return ((string)(this["WordsDeleteDefault"]));
            }
            set {
                this["WordsDeleteDefault"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int MaxPostErrors {
            get {
                return ((int)(this["MaxPostErrors"]));
            }
            set {
                this["MaxPostErrors"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string WordsBan {
            get {
                return ((string)(this["WordsBan"]));
            }
            set {
                this["WordsBan"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"{
	""words"" : [
		""продам"",
		""продажа"",
		""продаю акк"",
		""купить аккаунт"",
		""cTене"",
		""cTeHe"",
		""обсосок"",
		""ебучий"",
		""гандон"",
		""гондон"",
		""три бонус кода"",
		""продам акк"",
		""раздача бонус кодов"",
		""б*я"",
		""стеночке могу подарить"",
		""раздача баттлефиелд"",
		""нужны бонус-коды"",
		""хорошие аккаунты"",
		""заходите ко мне"",
		""по выгодным ценам"",
		""никакого обмана"",
		""нашел продавца"",
		""продавец"",
		""программа которая дала"",
		""игра которая покорила"",
		""ARMORED"",
		""купил и без акции"",
		""покупке акка"",
		""имею крутой акк"",
		""blogspot"",
		""Пиз*а"",
		""твари"",
		""РАЗДАЧА КОДОВ"",
		""СКИДКИ И ПОДАРКИ"",
		""ВСТУПАЙТЕ"",
		""лс не дорого"",
		""продажа аккаунтов"",
		""BONUS KODI"",
		""NA STENE"",
		""Y MENY"",
		""f7 f7 f7"",
		""еба"",
		""тварь"",
		""Зайдите ко мне"",
		""ебё*"",
		""ебе*"",
		""ебет"",
		""ебёт"",
		""Отдаю просто"",
		""себя на стене"",
		""на страничку"",
		""зарабатывать"",
		""запилил летсплей"",
		""захуярил"",
	]
}")]
        public string WordsBanDefault {
            get {
                return ((string)(this["WordsBanDefault"]));
            }
            set {
                this["WordsBanDefault"] = value;
            }
        }
    }
}
