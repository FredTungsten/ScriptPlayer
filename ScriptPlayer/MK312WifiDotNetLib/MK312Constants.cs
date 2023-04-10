using System;

namespace RexLabsWifiShock
{

    /// Implementation of the MK312 commands, basically just read and write byte
    public class MK312Constants {
                /// <summary>
        ///  Range 0x0000 - 0x00ff - Last page of Flash ROM (read only)
        /// </summary>
        public enum Flash : uint
        {
            BaseROM = 0x0000,                   // Partial String Table
            BaseData = 0x0098,                  // .data Segment
            BoxModel = 0x00fc,                  // Box Model
            VersionMajor = 0x00fd,              // Firmware Version
            VersionMinor = 0x00fe,              // Firmware Version
            VersionInternal = 0x00ff,           // Firmware Version
        }

        /// <summary>
        ///  Range 0x4000 - 0x43ff - Complete MAP of Microcontroller RAM (read/write)
        ///
        ///  Please note that these definitions only include locations
        ///  obviously useful in controlling the device. Please refer to
        ///  buttshock-protocol-docs for a more complete list of
        /// </summary>
        public enum RAM : uint
        {
            BaseRAM = 0x4000,                   // r0(CPU Register)
            r1 = 0x4001,                        // r1(CPU Register)
            r2 = 0x4002,                        // r2(CPU Register)
            r3 = 0x4003,                        // r3(CPU Register)
            r4 = 0x4004,                        // r4(CPU Register)
            r5 = 0x4005,                        // r5(CPU Register) copied from $4090
            r6 = 0x4006,                        // r6(CPU Register) copied from $409c
            r7 = 0x4007,                        // r7(CPU Register) copied from $40a5
            r8 = 0x4008,                        // r8 (CPU Register) copied from min(9, $40ae)
            r9 = 0x4009,                        // r9 (CPU Register) copied from min(50, $40b7)
            r10 = 0x400a,                       // r10(CPU Register) copied from $4190
            r11 = 0x400b,                       // r11(CPU Register) copied from $419c
            r12 = 0x400c,                       // r12(CPU Register) copied from $41a5
            r13 = 0x400d,                       // r13 (CPU Register) copied from min(9, $41ae)
            r14 = 0x400e,                       // r14 (CPU Register) copied from min(50, $41b7)
            SystemFlags = 0x400f,               // r15/ADC disable and other flags - COMM_SYSTEM_FLAG
            SlaveFlags = 0x4010,                // r16(CPU Register) various flags
            RunTimeFlags = 0x4011,              // r17(CPU Register) various flags
            r18 = 0x4012,                       // r18(CPU Register)
            ActionDown = 0x4013,                // r19(CPU Register) action when down key pushed
            ActionUp = 0x4014,                  // r20(CPU Register) action when up key pushed
            ActionMenu = 0x4015,                // r21(CPU Register) action when menu key pushed
            ActionOK = 0x4016,                  // r22(CPU Register) action when ok key pushed
            r23 = 0x4017,                       // r23(CPU Register)
            r24 = 0x4018,                       // r24(CPU Register)
            r25 = 0x4019,                       // r25(CPU Register)
            r26 = 0x401a,                       // r26(CPU Register)
            r27 = 0x401b,                       // r27(CPU Register)
            r28 = 0x401c,                       // r28(CPU Register)
            r29 = 0x401d,                       // r29(CPU Register)
            r30 = 0x401e,                       // r30(CPU Register)
            r31 = 0x401f,                       // r31(CPU Register)
            TWBR = 0x4020,                      // TWBR(IO Register)
            TWSR = 0x4021,                      // TWSR(IO Register)
            TWAR = 0x4022,                      // TWAR(IO Register)
            TWDR = 0x4023,                      // TWDR(IO Register)
            ADCL = 0x4024,                      // ADCL(IO Register)
            ADCH = 0x4025,                      // ADCH(IO Register)
            ADCSRA = 0x4026,                    // ADCSRA(IO Register)
            ADMUX = 0x4027,                     // ADMUX(IO Register)
            ACSR = 0x4028,                      // ACSR(IO Register)
            UBRRL = 0x4029,                     // UBRRL(IO Register, Baud Rate)
            UCSRB = 0x402a,                     // UCSRB(IO Register)
            UCSRA = 0x402b,                     // UCSRA(IO Register)
            UDR = 0x402c,                       // UDR(IO Register)
            SPCR = 0x402d,                      // SPCR(IO Register)
            SPSR = 0x402e,                      // SPSR(IO Register)
            SPDR = 0x402f,                      // SPDR(IO Register)
            PIND = 0x4030,                      // PIND(IO Register)
            DDRD = 0x4031,                      // DDRD(IO Register)
            PORTD = 0x4032,                     // PORTD(IO Register)
            PINC = 0x4033,                      // PINC(IO Register)
            DDRC = 0x4034,                      // DDRC(IO Register)
            PORTC = 0x4035,                     // PORTC(IO Register)
            PINB = 0x4036,                      // PINB(IO Register)
            DDRB = 0x4037,                      // DDRB(IO Register)
            PORTB = 0x4038,                     // PORTB(IO Register)
            PINA = 0x4039,                      // PINA(IO Register)
            DDRA = 0x403a,                      // DDRA(IO Register)
            PORTA = 0x403b,                     // PORTA(IO Register)
            EECR = 0x403c,                      // EECR(IO Register)
            EEDR = 0x403d,                      // EEDR(IO Register)
            EEARL = 0x403e,                     // EEARL(IO Register)
            EEARH = 0x403f,                     // EEARH(IO Register)
            UBRRH_UCSRC = 0x4040,               // UBRRH/UCSRC(IO Register)
            WDTCR = 0x4041,                     // WDTCR(IO Register)
            ASSR = 0x4042,                      // ASSR(IO Register)
            OCR2 = 0x4043,                      // OCR2(IO Register)
            TCNT2 = 0x4044,                     // TCNT2(IO Register)
            TCCR2 = 0x4045,                     // TCCR2(IO Register)
            ICR1L = 0x4046,                     // ICR1L(IO Register)
            ICR1H = 0x4047,                     // ICR1H(IO Register)
            OCR1BL = 0x4048,                    // OCR1BL(IO Register)
            OCR1BH = 0x4049,                    // OCR1BH(IO Register)
            OCR1AL = 0x404a,                    // OCR1AL(IO Register)
            OCR1AH = 0x404b,                    // OCR1AH(IO Register)
            TCNT1L = 0x404c,                    // TCNT1L(IO Register)
            TCNT1H = 0x404d,                    // TCNT1H(IO Register)
            TCCR1B = 0x404e,                    // TCCR1B(IO Register)
            TCCR1A = 0x404f,                    // TCCR1A(IO Register)
            SFIOR = 0x4050,                     // SFIOR(IO Register)
            OSCCAL_OCDR = 0x4051,               // OSCCAL/OCDR(IO Register)
            TCNT0 = 0x4052,                     // TCNT0(IO Register)
            TCCR0 = 0x4053,                     // TCCR0(IO Register)
            MCUCSR = 0x4054,                    // MCUCSR(IO Register)
            MCUCR = 0x4055,                     // MCUCR(IO Register)
            TWCR = 0x4056,                      // TWCR(IO Register)
            SPMCSR = 0x4057,                    // SPMCSR(IO Register)
            TIFR = 0x4058,                      // TIFR(IO Register)
            TIMSK = 0x4059,                     // TIMSK(IO Register)
            GIFR = 0x405a,                      // GIFR(IO Register)
            GICR = 0x405b,                      // GICR(IO Register)
            OCR0 = 0x405c,                      // OCR0(IO Register)
            SPL = 0x405d,                       // SPL(IO Register)
            SPH = 0x405e,                       // SPH(IO Register)
            SREG = 0x405f,                      // SREG(IO Register)
            SenseOutputCurrent = 0x4060,        // ADC0: Output Current Sense COMM_MAIN_CBLOCK_BASE
            SenseMAOffset = 0x4061,             // ADC1: Multi Adjust Offset - CBLOCK_MULTI_A_OFFSET
            SensePSUVoltage = 0x4062,           // ADC2: Power Supply Voltage
            SenseBatteryVoltage = 0x4063,       // ADC3: Battery Voltage
            SenseLevelPotA = 0x4064,            // ADC4: Level Pot A - CBLOCK_POT_A_OFFSET
            SenseLevelPotB = 0x4065,            // ADC5: Level Pot B - CBLOCK_POT_B_OFFSET
            SenseAudioLevelA = 0x4066,          // ADC6: Audio Input Level A (Half wave)
            SenseAudioLevelB = 0x4067,          // ADC7: Audio Input Level B (Half wave)
            SenseButtons = 0x4068,              // Current pushed buttons
            SenseButtonLatched = 0x4069,        // Last pushed buttons
            MasterTimerMSB = 0x406A,            // Master timer (MSB) (0x4073 LSB) runs 1.91Hz
            OutputCalibrationA = 0x406B,        // Channel A calibration (DAC power offset)
            OutputCalibrationB = 0x406C,        // Channel B calibration (DAC power offset)
            MenuState = 0x406D,                 // Menu State
            BoxCommand1 = 0x4070,               // Execute Command (1)
            BoxCommand2 = 0x4071,               // Execute Command (2)
            RNGOut = 0x4072,                    // Last random number picked
            MasterTimerLSB = 0x4073,            // Master timer (LSB) runs at 488Hz (8MHz/64(scaler)/256)
            MenuItemDisplayed = 0x4078,         // Current displayed Menu Item/Mode (not yet selected)
            MenuItemLowBoundary = 0x4079,       // Lowest Selectable Menu Item/Mode
            MenuItemHighBoundary = 0x407A,      // Highest Selectable Menu Item/Mode
            CurrentMode = 0x407b,               // Current Mode
            ChannelAOscillatorLow = 0x407c,     // Oscillator Ch A (updated but unused)
            ChannelAOscillatorHigh = 0x407d,    // Oscillator Ch A (updated but unused)
            ChannelBOscillatorLow = 0x407e,     // Oscillator Ch B (updated but unused)
            ChannelBOscillatorHigh = 0x407F,    // Oscillator Ch B (updated but unused)
            OutputFlags = 0x4083,               // Output Control Flags - COMM_CONTROL_FLAG (0x00)
            MultiAdjustRangeMin = 0x4086,       // Multi Adjust Range Min (0x0f)
            MultiAdjustRangeMax = 0x4087,       // Multi Adjust Range Max (0xff)
            ModuleTimerLow = 0x4088,            // Module timer (3 bytes) low - 244Hz (409uS)
            ModuleTimerMid = 0x4089,            // Module timer (3 bytes) mid - 0.953Hz (1.048S)
            ModuleTimerHigh = 0x408a,           // Module timer (3 bytes) high - (268.43S)
            ModuleTimerAlternative = 0x408b,    // Module timer (slower) - 30.5Hz
            ChannelAGateValue = 0x4090,         // Channel A: Current Gate Value (0x06)
            ChannelAGateOnTime = 0x4098,        // Channel A: Current Gate OnTime (0x3e)
            ChannelAGateOffTime = 0x4099,       // Channel A: Current Gate OffTime (0x3e)
            ChannelAGateSelect = 0x409a,        // Channel A: Current Gate Select (0x00)
            ChannelAGateCounter = 0x409b,       // Channel A: number of Gate transitions done (0x00)
            ChannelARampValue = 0x409c,         // Mode Switch Ramp Value Counter (0x9c)
            ChannelARampMin = 0x409d,           // Mode Switch Ramp Value Min (0x9c)
            ChannelARampMax = 0x409e,           // Mode Switch Ramp Value Max (0xff)
            ChannelARampRate = 0x409f,          // Mode Switch Ramp Value Rate (0x07)
            ChannelARampStep = 0x40a0,          // Mode Switch Ramp Value Step (0x01)
            ChannelARampAtMin = 0x40a1,         // Mode Switch Ramp Action at Min (0xfc)
            ChannelARampAtMax = 0x40a2,         // Mode Switch Ramp Action at Max (0xfc)
            ChannelARampSelect = 0x40a3,        // Mode Switch Ramp Select (0x01)
            ChannelARampTimer = 0x40a4,         // Mode Switch Ramp Current Timer (0x00)
            ChannelAIntensity = 0x40a5,         // Channel A: Current Intensity Modulation Value (0xff)
            ChannelAIntensityMin = 0x40a6,      // Channel A: Current Intensity Modulation Min (0xcd)
            ChannelAIntensityMax = 0x40a7,      // Channel A: Current Intensity Modulation Max (0xff)
            ChannelAIntensityRate = 0x40a8,     // Channel A: Current Intensity Modulation Rate (0x01)
            ChannelAIntensityStep = 0x40a9,     // Channel A: Current Intensity Modulation Step (0x01)
            ChannelAIntensityAtMin = 0x40aa,    // Channel A: Current Intensity Action at Min (0xff)
            ChannelAIntensityAtMax = 0x40ab,    // Channel A: Current Intensity Action at Max (0xff)
            ChannelAIntensitySelect = 0x40ac,   // Channel A: Current Intensity Modulation Select (0x00)
            ChannelAIntensityTimer = 0x40ad,    // Channel A: Current Intensity Modulation Timer (0x00)
            ChannelAFrequency = 0x40ae,         // Channel A: Current Frequency Modulation Value (0x16)
            ChannelAFrequencyMin = 0x40af,      // Channel A: Current Frequency Modulation Min (0x09)
            ChannelAFrequencyMax = 0x40b0,      // Channel A: Current Frequency Modulation Max (0x64)
            ChannelAFrequencyRate = 0x40b1,     // Channel A: Current Frequency Modulation Rate (0x01)
            ChannelAFrequencyStep = 0x40b2,     // Channel A: Current Frequency Modulation Step (0x01)
            ChannelAFrequencyAtMin = 0x40b3,    // Channel A: Current Frequency Modulation Action Min (0xff)
            ChannelAFrequencyAtMax = 0x40b4,    // Channel A: Current Frequency Modulation Action Max (0xff)
            ChannelAFrequencySelect = 0x40b5,   // Channel A: Current Frequency Modulation Select (0x08)
            ChannelAFrequencyTimer = 0x40b6,    // Channel A: Current Frequency Modulation Timer (0x00)
            ChannelAWidth = 0x40b7,             // Channel A: Current Width Modulation Value (0x82)
            ChannelAWidthMin = 0x40b8,          // Channel A: Current Width Modulation Min (0x32)
            ChannelAWidthMax = 0x40b9,          // Channel A: Current Width Modulation Max (0xc8)
            ChannelAWidthRate = 0x40ba,         // Channel A: Current Width Modulation Rate (0x01)
            ChannelAWidthStep = 0x40bb,         // Channel A: Current Width Modulation Step (0x01)
            ChannelAWidthAtMin = 0x40bc,        // Channel A: Current Width Modulation Action Min (0xff)
            ChannelAWidthAtMax = 0x40bd,        // Channel A: Current Width Modulation Action Max (0xff)
            ChannelAWidthSelect = 0x40be,       // Channel A: Current Width Modulation Select (0x04)
            ChannelAWidthTimer = 0x40bf,        // Channel A: Current Width Modulation Timer (0x00)
            ScratchPadA = 0x40c0,               // Space for User Module Scratchpad A
            WriteLCDParameter = 0x4180,         // Write LCD Parameter
            WriteLCDPosition = 0x4181,          // Write LCD Position
            BoxCommandParameter1 = 0x4182,      // Parameter r26 for box command
            BoxCommandParameter2 = 0x4183,      // Parameter r27 for box command
            ChanneBGateValue = 0x4190,          // Channel B: Current Gate Value (0 when no output)
            ChannelBGateOnTime = 0x4198,        // Channel B: Current Gate OnTime (0x3e)
            ChannelBGateOffTime = 0x4199,       // Channel B: Current Gate OffTime (0x3e)
            ChannelBGateSelect = 0x419a,        // Channel B: Current Gate Select (0x00)
            ChannelBGateCounter = 0x419b,       // Channel B: number of Gate transitions done (0x00)
            ChannelBRampValue = 0x419c,         // Mode Switch Ramp Value Counter (0x9c)
            ChannelBRampMin = 0x419d,           // Mode Switch Ramp Value Min (0x9c)
            ChannelBRampMax = 0x419e,           // Mode Switch Ramp Value Max (0xff)
            ChannelBRampRate = 0x419f,          // Mode Switch Ramp Value Rate (0x07)
            ChannelBRampStep = 0x41a0,          // Mode Switch Ramp Value Step (0x01)
            ChannelBRampAtMin = 0x41a1,         // Mode Switch Ramp Action at Min (0xfc)
            ChannelBRampAtMax = 0x41a2,         // Mode Switch Ramp Action at Max (0xfc)
            ChannelBRampSelect = 0x41a3,        // Mode Switch Ramp Select (0x01)
            ChannelBRampTimer = 0x41a4,         // Mode Switch Ramp Current Timer (0x00)
            ChannelBIntensity = 0x41a5,         // Channel B: Current Intensity Modulation Value (0xff)
            ChannelBIntensityMin = 0x41a6,      // Channel B: Current Intensity Modulation Min (0xcd)
            ChannelBIntensityMax = 0x41a7,      // Channel B: Current Intensity Modulation Max (0xff)
            ChannelBIntensityRate = 0x41a8,     // Channel B: Current Intensity Modulation Rate (0x01)
            ChannelBIntensityStep = 0x41a9,     // Channel B: Current Intensity Modulation Step (0x01)
            ChannelBIntensityAtMin = 0x41aa,    // Channel B: Current Intensity Action at Min (0xff)
            ChannelBIntensityAtMax = 0x41ab,    // Channel B: Current Intensity Action at Max (0xff)
            ChannelBIntensitySelect = 0x41ac,   // Channel B: Current Intensity Modulation Select (0x00)
            ChannelBIntensityTimer = 0x41ad,    // Channel B: Current Intensity Modulation Timer (0x00)
            ChannelBFrequency = 0x41ae,         // Channel B: Current Frequency Modulation Value (0x16)
            ChannelBFrequencyMin = 0x41af,      // Channel B: Current Frequency Modulation Min (0x09)
            ChannelBFrequencyMax = 0x41b0,      // Channel B: Current Frequency Modulation Max (0x64)
            ChannelBFrequencyRate = 0x41b1,     // Channel B: Current Frequency Modulation Rate (0x01)
            ChannelBFrequencyStep = 0x41b2,     // Channel B: Current Frequency Modulation Step (0x01)
            ChannelBFrequencyAtMin = 0x41b3,    // Channel B: Current Frequency Modulation Action Min (0xff)
            ChannelBFrequencyAtMax = 0x41b4,    // Channel B: Current Frequency Modulation Action Max (0xff)
            ChannelBFrequencySelect = 0x41b5,   // Channel B: Current Frequency Modulation Select (0x08)
            ChannelBFrequencyTimer = 0x41b6,    // Channel B: Current Frequency Modulation Timer (0x00)
            ChannelBWidth = 0x41b7,             // Channel B: Current Width Modulation Value (0x82)
            ChannelBWidthMin = 0x41b8,          // Channel B: Current Width Modulation Min (0x32)
            ChannelBWidthMax = 0x41b9,          // Channel B: Current Width Modulation Max (0xc8)
            ChannelBWidthRate = 0x41ba,         // Channel B: Current Width Modulation Rate (0x01)
            ChannelBWidthStep = 0x41bb,         // Channel B: Current Width Modulation Step (0x01)
            ChannelBWidthAtMin = 0x41bc,        // Channel B: Current Width Modulation Action Min (0xff)
            ChannelBWidthAtMax = 0x41bd,        // Channel B: Current Width Modulation Action Max (0xff)
            ChannelBWidthSelect = 0x41be,       // Channel B: Current Width Modulation Select (0x04)
            ChanneBAWidthTimer = 0x41bf,        // Channel B: Current Width Modulation Timer (0x00)
            AverageMASamples = 0x41c0,          // last 16 MA knob readings used for averaging
            ScratchPadPointers = 0x41d0,        // User Module Scratchpad Pointers
            TopMode = 0x41f3,                   // CurrentTopMode (written during routine write) (0x87)
            PowerLevel = 0x41f4,                // PowerLevel - COMM_POWER_LEVEL / COMM_LMODE (0x02)
            SplitModeA = 0x41f5,                // Split Mode Number A (0x77)
            SplitModeB = 0x41f6,                // Split Mode Number B (0x76)
            FavouriteMode = 0x41f7,             // Favourite Mode (0x76)
            AdvancedRampLevel = 0x41f8,         // Advanced Parameter: RampLevel (0xe1)
            AdvancedRampTime = 0x41f9,          // Advanced Parameter: RampTime (0x14)
            AdvancedDepth = 0x41fa,             // Advanced Parameter: Depth (0xd7)
            AdvancedTempo = 0x41fb,             // Advanced Parameter: Tempo (0x01)
            AdvancedFrequency = 0x41fc,         // Advanced Parameter: Frequency (0x19)
            AdvancedEffect = 0x41fd,            // Advanced Parameter: Effect (0x05)
            AdvancedWidth = 0x41fe,             // Advanced Parameter: Width (0x82)
            AdvancedPace = 0x41ff,              // Advanced Parameter: Pace (0x05)
            DebugEnable = 0x4207,               // debug mode: displays current module number if not 0
            SenseMultiAdjust = 0x420d,          // Current Multi Adjust Value / COMM_MULTI_AVG
            BoxKey = 0x4213,                    // com cipher key
            PowerSupply = 0x4215,               // power status bits
            ModuleParsed = 0x4218,              // decoded module instruction to parse
            ComBufferRX = 0x4220,               // serial comms input buffer
            ComBufferTX = 0x422c,               // serial comms output buffer
            UnusedRAM = 0x4238,                 // unused
        }

        /// <summary>
        ///  Range 0x8000 - 0x81ff - Complete MAP of Microcontroller EEPROM (read/write)
        /// </summary>
        public enum EEPROM : uint
        {
            BaseEEPROM = 0x8000,                // unused
            IsProvisioned = 0x8001,             // Magic (0x55 means we’re provisioned)
            BoxSerialLow = 0x8002,              // Box Serial 1
            BoxSerialHigh = 0x8003,             // Box Serial 2
            ELinkSig1 = 0x8006,                 // ELinkSig1 - ELINK_SIG1_ADDR (default 0x01)
            ELinkSig2 = 0x8007,                 // ELinkSig2 - ELINK_SIG2_ADDR (default 0x01)
            TopModeNV = 0x8008,                 // TopMode NonVolatile (written during routine write)
            PowerLevelNV = 0x8009,              // Power Level
            SplitModeANV = 0x800A,              // Split A Mode Num
            SplitModeBNV = 0x800B,              // Split B Mode Num
            FavouriteModeNV = 0x800C,           // Favourite Mode
            AdvancedRampLevelNV = 0x800D,       // Advanced Parameter: RampLevel
            AdvancedRampTimeNV = 0x800E,        // Advanced Parameter: RampTime
            AdvancedDepthNV = 0x800F,           // Advanced Parameter: Depth
            AdvancedTempoNV = 0x8010,           // Advanced Parameter: Tempo
            AdvancedFrequencyNV = 0x8011,       // Advanced Parameter: Frequency
            AdvancedEffectNV = 0x8012,          // Advanced Parameter: Effect
            AdvancedWidthNV = 0x8013,           // Advanced Parameter: Width
            AdvancedPaceNV = 0x8014,            // Advanced Parameter: Pace
            UserRoutineVector1 = 0x8018,        // Start Vector User 1 - COMM_USER_BASE
            UserRoutineVector2 = 0x8019,        // Start Vector User 2
            UserRoutineVector3 = 0x801A,        // Start Vector User 3
            UserRoutineVector4 = 0x801B,        // Start Vector User 4
            UserRoutineVector5 = 0x801C,        // Start Vector User 5
            UserRoutineVector6 = 0x801D,        // Start Vector User 6
            UserRoutineVector7 = 0x801E,        // Start Vector User 7 (not implemented)
            UserRoutineVector8 = 0x801F,        // Start Vector User 8 (not implemented)
            UserRoutinePointersA = 0x8020,      // User routine module pointers 0x80-0x9f
            UserSpaceA = 0x8040,                // Space for User Modules
            UserRoutinePointersB = 0x8100,      // User routine module pointers 0xa0-0xbf
            UserSpaceB = 0x8120,                // Space for User Modules
        }

        /// <summary>
        /// Possible bit field values for SystemFlags
        /// </summary>
        [Flags]
        public enum SystemFlags : byte
        {
            DisableADC = 0x01,          // Disable ADC (pots etc) (SYSTEM_FLAG_POTS_DISABLE_MASK)
            Jump = 0x02,                // If set then we jump to a new module number given in $4084
            Shareable = 0x04,           // Can this program be shared with a slave unit
            DisableMultiAdjust = 0x08,  // Disable Multi Adjust(SYSTEM_FLAG_MULTIA_POT_DISABLE_MASK)
        }

        /// <summary>
        /// Possible bit field values for SlaveFlags
        /// </summary>
        [Flags]
        public enum SlaveFlags : byte
        {
            Linked = 0x04,              // set if we are a linked slave
            Select = 0x40,              // in slave mode determines which registers to send(toggles)
        }

        /// <summary>
        /// Possible bit field values for RuntimeFlags
        /// </summary>
        [Flags]
        public enum RuntimeFlags : byte
        {
            ApplyToA = 0x01,            // when module loading to apply module to channel A
            ApplyToAB = 0x02,           // when module loading to apply module to channel B
            Triggered = 0x04,           // used to tell main code that the timer has triggered
            ADCConv = 0x08,             // set while ADC conversion is running
            BoxReceived = 0x20,         // set if received a full serial command to parse
            SerialError = 0x40,         // set if serial comms error
            MasterMode = 0x80,          // set if we are a linked master
        }

        /// <summary>
        /// Possible values for MenuState
        /// </summary>
        public enum MenuState : byte
        {
            Active = 0x01,              // In startup screen or in a menu
            InActive = 0x02,            // No menu, proram is running and displaying"
        }

        /// <summary>
        /// Commands to be executed via BoxCommand1 and BoxCommand2
        /// </summary>
        public enum BoxCommand : byte
        {
            NOP = 0x01,                 // do nothing
            StatusMenu = 0x02,          // Display Status Screen
            Select = 0x03,              // Select current Menu Item
            Exit = 0x04,                // Exit Menu
            FavouriteMode = 0x05,       // Same as 0x00
            SetPowerMenu = 0x06,        // Set Power Level
            AdvancedSelect = 0x07,      // Edit Advanced Parameter
            NextItem = 0x08,            // display next menu item
            PreviousItem = 0x09,        // display previous menu item
            MainMenu = 0x0a,            // Show Main Menu
            SplitMenu = 0x0b,           // Jump to split mode settings menu
            SplitMode = 0x0c,           // Activates Split Mode
            ValueUp = 0x0d,             // Advanced Value Up
            ValueDown = 0x0e,           // Advanced Value Down
            AdvancedMenu = 0x0f,        // Show Advanced Menu
            NextMode = 0x10,            // Switch to Next mode
            PreviousMode = 0x11,        // Switch to Previous mode
            SetMode = 0x12,             // New Mode
            LCDWriteCharacter = 0x13,   // Write Character to LCD
            LCDWriteNumber = 0x14,      // Write Number to LCD
            LCDWriteString = 0x15,      // Write String from Stringtable to LCD
            LoadModule = 0x16,          // Load module
            Mute = 0x18,                // Clear module (Mute)
            SwapChannel = 0x19,         // Swap Channel A and B
            CopyAtoB = 0x1a,            // Copy Channel A to Channel B
            CopyBtoA = 0x1b,            // Copy Channel B to Channel A
            LoadDefaults = 0x1c,        // Copy defaults from EEPROM
            SetUpRegisters = 0x1d,      // Sets up running module registers
            SingleInstruction = 0x1e,   // Handles single instruction from a module
            FunctionCall = 0x1f,        // General way to call these functions
            UpdateAdvanced = 0x20,      // Advanced Setting Update
            StartRamp = 0x21,           // Start Ramp
            StartADC = 0x22,            // Does an ADC conversion
            LCDSetPosition = 0x23,      // Set LCD position
            None = 0xff,                // No command
        }

        /// <summary>
        /// Possible values for all Mode Selection settings
        /// </summary>
        public enum Mode : byte
        {
            PowerOn = 0x00,         // MODE_NUM_POWER_ON
            Unknown = 0x01,         // MODE_NUM_UNKNOWN
            Waves = 0x76,           // MODE_NUM_WAVES / MODE_NUM_LOWER
            Stroke = 0x77,          // MODE_NUM_STROKE
            Climb = 0x78,           // MODE_NUM_CLIMB
            Combo = 0x79,           // MODE_NUM_COMBO
            Intense = 0x7a,         // MODE_NUM_INTENSE
            Rythm = 0x7b,           // MODE_NUM_RHYTHM
            Audio1 = 0x7c,          // MODE_NUM_AUDIO1
            Audio2 = 0x7d,          // MODE_NUM_AUDIO2
            Audio3 = 0x7e,          // MODE_NUM_AUDIO3
            Split = 0x7f,           // MODE_NUM_SPLIT
            Random1 = 0x80,         // MODE_NUM_RANDOM1
            Random2 = 0x81,         // MODE_NUM_RANDOM2
            Toggle = 0x82,          // MODE_NUM_TOGGLE
            rOrgasm = 0x83,         // MODE_NUM_ORGASM
            Torment = 0x84,         // MODE_NUM_TORMENT
            Phase1 = 0x85,          // MODE_NUM_PHASE1
            Phase2 = 0x86,          // MODE_NUM_PHASE2
            Phase3 = 0x87,          // MODE_NUM_PHASE3
            User1 = 0x88,           // MODE_NUM_USER1
            User2 = 0x89,           // MODE_NUM_USER2
            User3 = 0x8a,           // MODE_NUM_USER3
            User4 = 0x8b,           // MODE_NUM_USER4
            User5 = 0x8c,           // MODE_NUM_USER5
            User6 = 0x8d,           // MODE_NUM_USER6
            User7 = 0x8e,           // MODE_NUM_USER7 / MODE_NUM_UPPER
        }

        /// <summary>
        /// Possible bit field values for OutputFlags
        /// </summary>
        [Flags]
        public enum OutputFlags : byte
        {
            Phase1 = 0x01,          // Phase Control
            Mute = 0x02,            // Mute
            Phase2 = 0x04,          // Phase Control 2
            Phase3 = 0x08,          // Phase Control 3
            DisableControls = 0x20, // Disable Frontpanel Switches
            Mono = 0x40,            // Mono Mode (off=Stereo)
        }

        /// <summary>
        /// Possible bit field values for all "Gate Select" settings
        /// </summary>
        [Flags]
        public enum Gate : byte
        {
            Off = 0x00,             // No gating
            TimerFast = 0x01,       // Use the $4088 (244Hz) timer for gating
            TimerMedium = 0x02,     // Use the $4088 div 8 (30.5Hz) timer for gating
            TimerSlow = 0x03,       // Use the $4089 (.953Hz) timer for gating
            OffFromTempo = 0x04,    // Off time is taken from the advanced parameter tempo default
            OffFromMA = 0x08,       // Off time follows the value of the MA knob
            OnFromEffect = 0x20,    // On time is taken from the advanced parameter effect default
            OnFromMA = 0x40,        // On time follows the value of the MA knob
        }

        /// <summary>
        /// Possible bit field values for Frequency/Width/Intensity Ramp "AtMin" and "AtMax" settings
        /// </summary>
        [Flags]
        public enum Ramp : byte
        {
            Stop = 0xfc,            // Stop when ramp min/max is reached
            Loop = 0xfd,            // Loop back round (if below min, set to max; if above max set to min
            ToggleGate = 0xfc,      // Reverse direction, toggle gate and continue"
            Reverse = 0xff,         // Reverse Direction ramp min/max is reached
        }

        /// <summary>
        /// Possible bit field values for Frequency/Width/Intensity Select settings
        /// </summary>
        [Flags]
        public enum Select : byte
        {
            Static = 0x00,          // Set the value to an absolute value determined by the other bits
            TimerFast = 0x01,       // Update the value based on timer at $4088 (244Hz)
            TimerMedium = 0x02,     // Update the value based on timer at $4088 divided by 8 (30.5Hz)
            TimerSlow = 0x03,       // Update the value based on timer at $4089 (.953Hz
            Advanced = 0x04,        // Set the value to advanced_parameter default for this variable
            MA = 0x08,              // Set the value to the current MA knob value
            Other = 0x0c,           // Copy from the other channels value
            AdvancedInverted = 0x14, // Set the value to the inverse of the advanced_parameter default
            MAInverted = 0x18,      // Set the value to the inverse of the current MA knob value
            OtherInverted = 0x1c,   // Inverse of the other channels value
            RateAdvanced = 0x20,    // Rate is from advanced_parameter default
            RateMA = 0x40,          // Rate is from MA value
            RateOther = 0x60,       // Rate is rate from other channel
            RateAbsInverted = 0x80, // Rate is inverse of parameter (example $40ba)
            RateAdvancedInverted = 0xa0, // Rate is inverse of advanced_parameter default
            RateMAInverted = 0xc0,  // Rate is inverse of MA value
            RateOtherInverted = 0xf0, // Rate is inverse of rate from other channel
        }

        /// <summary>
        /// Possible bit field values for PowerLevel and PowerLevelNV
        /// </summary>
        public enum PowerLevel : byte
        {
            Low = 0x00,             // LOW
            Normal = 0x01,          // NORMAL
            High = 0x02,            // HIGH
        }

        /// <summary>
        /// Possible bit field values for PowerSupply
        /// </summary>
        [Flags]
        public enum PowerSupply : byte
        {
            Battery = 0x01,         // Set if we have a battery
            PSU = 0x02,             // Set if we have a PSU connected
        }

        /// <summary>
        /// Serial Port Commands (Client -> Box)
        /// </summary>
        public enum SerialCommand : byte
        {
            Sync = 0x00,            // Dummy Command (always fails)
            Reset = 0x08,           // Reset box to power on defaults
            Read = 0x0c,            // Read from box memory
            Write = 0x0d,           // Write to box memory
            Master = 0x0e,          // Request link mode
            KeyExchange = 0x0f,     // Request key exchange
        }

        /// <summary>
        /// Serial Port Responses (Box -> Client)
        /// </summary>
        public enum SerialResponse : byte
        {
            KeyExchange = 0x01,     // Key exchange acknowledged
            Read = 0x02,            // Result of read operation
            Slave = 0x05,           // Link mode acknowledged
            OK = 0x06,              // Command executed successfully
            Error = 0x07,           // Command not executed sucessfully
        }
    }

}