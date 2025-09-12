﻿/* Copyright (C) 2025 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using IBApi.protobuf;

namespace IBApi
{
    internal class EDecoderUtils
    {
        public static Contract decodeContract(protobuf.Contract contractProto)
        {
            Contract contract = new Contract();
            if (contractProto.HasConId) contract.ConId = contractProto.ConId;
            if (contractProto.HasSymbol) contract.Symbol = contractProto.Symbol;
            if (contractProto.HasSecType) contract.SecType = contractProto.SecType;
            if (contractProto.HasLastTradeDateOrContractMonth) contract.LastTradeDateOrContractMonth = contractProto.LastTradeDateOrContractMonth;
            if (contractProto.HasStrike) contract.Strike = contractProto.Strike;
            if (contractProto.HasRight) contract.Right = contractProto.Right;
            if (contractProto.HasMultiplier) contract.Multiplier = Util.DoubleMaxString(contractProto.Multiplier);
            if (contractProto.HasExchange) contract.Exchange = contractProto.Exchange;
            if (contractProto.HasCurrency) contract.Currency = contractProto.Currency;
            if (contractProto.HasLocalSymbol) contract.LocalSymbol = contractProto.LocalSymbol;
            if (contractProto.HasTradingClass) contract.TradingClass = contractProto.TradingClass;
            if (contractProto.HasComboLegsDescrip) contract.ComboLegsDescription = contractProto.ComboLegsDescrip;

            List<ComboLeg> comboLegs = decodeComboLegs(contractProto); 
            if (comboLegs != null && comboLegs.Any()) contract.ComboLegs = comboLegs;
            DeltaNeutralContract deltaNeutralContract = decodeDeltaNeutralContract(contractProto);
            if (deltaNeutralContract != null) contract.DeltaNeutralContract = deltaNeutralContract;

            if (contractProto.HasLastTradeDate) contract.LastTradeDate = contractProto.LastTradeDate;
            if (contractProto.HasPrimaryExch) contract.PrimaryExch = contractProto.PrimaryExch;

            return contract;
        }

        public static List<ComboLeg> decodeComboLegs(protobuf.Contract contractProto) 
        {
            List<ComboLeg> comboLegs = null;

            if (contractProto.ComboLegs.Count > 0) 
            {
                List<protobuf.ComboLeg> comboLegProtoList = new List<protobuf.ComboLeg>();
                comboLegProtoList.AddRange(contractProto.ComboLegs);
                comboLegs = new List<ComboLeg>();

                foreach (protobuf.ComboLeg comboLegProto in comboLegProtoList) 
                {
                    ComboLeg comboLeg = new ComboLeg();
                    if (comboLegProto.HasConId) comboLeg.ConId = comboLegProto.ConId;
                    if (comboLegProto.HasRatio) comboLeg.Ratio = comboLegProto.Ratio;
                    if (comboLegProto.HasAction) comboLeg.Action = comboLegProto.Action;
                    if (comboLegProto.HasExchange) comboLeg.Exchange = comboLegProto.Exchange;
                    if (comboLegProto.HasOpenClose) comboLeg.OpenClose = comboLegProto.OpenClose;
                    if (comboLegProto.HasShortSalesSlot) comboLeg.ShortSaleSlot = comboLegProto.ShortSalesSlot;
                    if (comboLegProto.HasDesignatedLocation) comboLeg.DesignatedLocation = comboLegProto.DesignatedLocation;
                    if (comboLegProto.HasExemptCode) comboLeg.ExemptCode = comboLegProto.ExemptCode;
                    comboLegs.Add(comboLeg);
                }
           }
            return comboLegs;
        }

        public static List<OrderComboLeg> decodeOrderComboLegs(protobuf.Contract contractProto) 
        {
            List<OrderComboLeg> orderComboLegs = null;
            if (contractProto.ComboLegs.Count > 0)
            { 
                orderComboLegs = new List<OrderComboLeg>();

                List<protobuf.ComboLeg> comboLegProtoList = new List<protobuf.ComboLeg>();
                comboLegProtoList.AddRange(contractProto.ComboLegs);
                foreach (protobuf.ComboLeg comboLegProto in comboLegProtoList)
                { 
                    OrderComboLeg orderComboLeg = comboLegProto.HasPerLegPrice ? new OrderComboLeg(comboLegProto.PerLegPrice) : new OrderComboLeg();
                    orderComboLegs.Add(orderComboLeg);
                }
            }
            return orderComboLegs;
        }

        public static DeltaNeutralContract decodeDeltaNeutralContract(protobuf.Contract contractProto)
        {
            DeltaNeutralContract deltaNeutralContract = null;
            protobuf.DeltaNeutralContract deltaNeutralContractProto = contractProto.DeltaNeutralContract;
            if (deltaNeutralContractProto != null)
            {
                deltaNeutralContract = new DeltaNeutralContract();
                if (deltaNeutralContractProto.HasConId) deltaNeutralContract.ConId = deltaNeutralContractProto.ConId;
                if (deltaNeutralContractProto.HasDelta) deltaNeutralContract.Delta = deltaNeutralContractProto.Delta;
                if (deltaNeutralContractProto.HasPrice) deltaNeutralContract.Price = deltaNeutralContractProto.Price;
            }
            return deltaNeutralContract;
        }

        public static Execution decodeExecution(protobuf.Execution executionProto)
        {
            Execution execution = new Execution();
            if (executionProto.HasOrderId) execution.OrderId = executionProto.OrderId;
            if (executionProto.HasClientId) execution.ClientId = executionProto.ClientId;
            if (executionProto.HasExecId) execution.ExecId = executionProto.ExecId;
            if (executionProto.HasTime) execution.Time = executionProto.Time;
            if (executionProto.HasAcctNumber) execution.AcctNumber = executionProto.AcctNumber;
            if (executionProto.HasExchange) execution.Exchange = executionProto.Exchange;
            if (executionProto.HasSide) execution.Side = executionProto.Side;
            if (executionProto.HasShares) execution.Shares = Util.StringToDecimal(executionProto.Shares);
            if (executionProto.HasPrice) execution.Price = executionProto.Price;
            if (executionProto.HasPermId) execution.PermId = executionProto.PermId;
            if (executionProto.HasIsLiquidation) execution.Liquidation = executionProto.IsLiquidation ? 1 : 0;
            if (executionProto.HasCumQty) execution.CumQty = Util.StringToDecimal(executionProto.CumQty);
            if (executionProto.HasAvgPrice) execution.AvgPrice = executionProto.AvgPrice;
            if (executionProto.HasOrderRef) execution.OrderRef = executionProto.OrderRef;
            if (executionProto.HasEvRule) execution.EvRule = executionProto.EvRule;
            if (executionProto.HasEvMultiplier) execution.EvMultiplier = executionProto.EvMultiplier;
            if (executionProto.HasModelCode) execution.ModelCode = executionProto.ModelCode;
            if (executionProto.HasLastLiquidity) execution.LastLiquidity = new Liquidity(executionProto.LastLiquidity);
            if (executionProto.HasIsPriceRevisionPending) execution.PendingPriceRevision = executionProto.IsPriceRevisionPending;
            if (executionProto.HasSubmitter) execution.Submitter = executionProto.Submitter;
            if (executionProto.HasOptExerciseOrLapseType) execution.OptExerciseOrLapseType = COptionExerciseType.getOptionExerciseType(executionProto.OptExerciseOrLapseType);
            return execution;
        }

        public static Order decodeOrder(int orderId, protobuf.Contract contractProto, protobuf.Order orderProto) 
        {
            Order order = new Order();
            if (Util.IsValidValue(orderId)) order.OrderId = orderId;
            if (orderProto.HasOrderId) order.OrderId = orderProto.OrderId;
            if (orderProto.HasAction) order.Action = orderProto.Action;
            if (orderProto.HasTotalQuantity) order.TotalQuantity = Util.StringToDecimal(orderProto.TotalQuantity);
            if (orderProto.HasOrderType) order.OrderType = orderProto.OrderType;
            if (orderProto.HasLmtPrice) order.LmtPrice = orderProto.LmtPrice;
            if (orderProto.HasAuxPrice) order.AuxPrice = orderProto.AuxPrice;
            if (orderProto.HasTif) order.Tif = orderProto.Tif;
            if (orderProto.HasOcaGroup) order.OcaGroup = orderProto.OcaGroup;
            if (orderProto.HasAccount) order.Account = orderProto.Account;
            if (orderProto.HasOpenClose) order.OpenClose = orderProto.OpenClose;
            if (orderProto.HasOrigin) order.Origin = orderProto.Origin;
            if (orderProto.HasOrderRef) order.OrderRef = orderProto.OrderRef;
            if (orderProto.HasClientId) order.ClientId = orderProto.ClientId;
            if (orderProto.HasPermId) order.PermId = orderProto.PermId;
            if (orderProto.HasOutsideRth) order.OutsideRth = orderProto.OutsideRth;
            if (orderProto.HasHidden) order.Hidden = orderProto.Hidden;
            if (orderProto.HasDiscretionaryAmt) order.DiscretionaryAmt = orderProto.DiscretionaryAmt;
            if (orderProto.HasGoodAfterTime) order.GoodAfterTime = orderProto.GoodAfterTime;
            if (orderProto.HasFaGroup) order.FaGroup = orderProto.FaGroup;
            if (orderProto.HasFaMethod) order.FaMethod = orderProto.FaMethod;
            if (orderProto.HasFaPercentage) order.FaPercentage = orderProto.FaPercentage;
            if (orderProto.HasModelCode) order.ModelCode = orderProto.ModelCode;
            if (orderProto.HasGoodTillDate) order.GoodTillDate = orderProto.GoodTillDate;
            if (orderProto.HasRule80A) order.Rule80A = orderProto.Rule80A;
            if (orderProto.HasPercentOffset) order.PercentOffset = orderProto.PercentOffset;
            if (orderProto.HasSettlingFirm) order.SettlingFirm = orderProto.SettlingFirm;
            if (orderProto.HasShortSaleSlot) order.ShortSaleSlot = orderProto.ShortSaleSlot;
            if (orderProto.HasDesignatedLocation) order.DesignatedLocation = orderProto.DesignatedLocation;
            if (orderProto.HasExemptCode) order.ExemptCode = orderProto.ExemptCode;
            if (orderProto.HasStartingPrice) order.StartingPrice = orderProto.StartingPrice;
            if (orderProto.HasStockRefPrice) order.StockRefPrice = orderProto.StockRefPrice;
            if (orderProto.HasDelta) order.Delta = orderProto.Delta;
            if (orderProto.HasStockRangeLower) order.StockRangeLower = orderProto.StockRangeLower;
            if (orderProto.HasStockRangeUpper) order.StockRangeUpper = orderProto.StockRangeUpper;
            if (orderProto.HasDisplaySize) order.DisplaySize = orderProto.DisplaySize;
            if (orderProto.HasBlockOrder) order.BlockOrder = orderProto.BlockOrder;
            if (orderProto.HasSweepToFill) order.SweepToFill = orderProto.SweepToFill;
            if (orderProto.HasAllOrNone) order.AllOrNone = orderProto.AllOrNone;
            if (orderProto.HasMinQty) order.MinQty = orderProto.MinQty;
            if (orderProto.HasOcaType) order.OcaType = orderProto.OcaType;
            if (orderProto.HasParentId) order.ParentId = orderProto.ParentId;
            if (orderProto.HasTriggerMethod) order.TriggerMethod = orderProto.TriggerMethod;
            if (orderProto.HasVolatility) order.Volatility = orderProto.Volatility;
            if (orderProto.HasVolatilityType) order.VolatilityType = orderProto.VolatilityType;
            if (orderProto.HasDeltaNeutralOrderType) order.DeltaNeutralOrderType = orderProto.DeltaNeutralOrderType;
            if (orderProto.HasDeltaNeutralAuxPrice) order.DeltaNeutralAuxPrice = orderProto.DeltaNeutralAuxPrice;
            if (orderProto.HasDeltaNeutralConId) order.DeltaNeutralConId = orderProto.DeltaNeutralConId;
            if (orderProto.HasDeltaNeutralSettlingFirm) order.DeltaNeutralSettlingFirm = orderProto.DeltaNeutralSettlingFirm;
            if (orderProto.HasDeltaNeutralClearingAccount) order.DeltaNeutralClearingAccount = orderProto.DeltaNeutralClearingAccount;
            if (orderProto.HasDeltaNeutralClearingIntent) order.DeltaNeutralClearingIntent = orderProto.DeltaNeutralClearingIntent;
            if (orderProto.HasDeltaNeutralOpenClose) order.DeltaNeutralOpenClose = orderProto.DeltaNeutralOpenClose;
            if (orderProto.HasDeltaNeutralShortSale) order.DeltaNeutralShortSale = orderProto.DeltaNeutralShortSale;
            if (orderProto.HasDeltaNeutralShortSaleSlot) order.DeltaNeutralShortSaleSlot = orderProto.DeltaNeutralShortSaleSlot;
            if (orderProto.HasDeltaNeutralDesignatedLocation) order.DeltaNeutralDesignatedLocation = orderProto.DeltaNeutralDesignatedLocation;
            if (orderProto.HasContinuousUpdate) order.ContinuousUpdate = orderProto.ContinuousUpdate ? 1 : 0;
            if (orderProto.HasReferencePriceType) order.ReferencePriceType = orderProto.ReferencePriceType;
            if (orderProto.HasTrailStopPrice) order.TrailStopPrice = orderProto.TrailStopPrice;
            if (orderProto.HasTrailingPercent) order.TrailingPercent = orderProto.TrailingPercent;

            List<OrderComboLeg> orderComboLegs = decodeOrderComboLegs(contractProto);
            if (orderComboLegs != null) order.OrderComboLegs = orderComboLegs;

            if (orderProto.SmartComboRoutingParams.Count > 0) 
            {
                List<TagValue> smartComboRoutingParams = decodeTagValueList(orderProto.SmartComboRoutingParams);
                order.SmartComboRoutingParams = smartComboRoutingParams;
            }

            if (orderProto.HasScaleInitLevelSize) order.ScaleInitLevelSize = orderProto.ScaleInitLevelSize;
            if (orderProto.HasScaleSubsLevelSize) order.ScaleSubsLevelSize = orderProto.ScaleSubsLevelSize;
            if (orderProto.HasScalePriceIncrement) order.ScalePriceIncrement = orderProto.ScalePriceIncrement;
            if (orderProto.HasScalePriceAdjustValue) order.ScalePriceAdjustValue = orderProto.ScalePriceAdjustValue;
            if (orderProto.HasScalePriceAdjustInterval) order.ScalePriceAdjustInterval = orderProto.ScalePriceAdjustInterval;
            if (orderProto.HasScaleProfitOffset) order.ScaleProfitOffset = orderProto.ScaleProfitOffset;
            if (orderProto.HasScaleAutoReset) order.ScaleAutoReset = orderProto.ScaleAutoReset;
            if (orderProto.HasScaleInitPosition) order.ScaleInitPosition = orderProto.ScaleInitPosition;
            if (orderProto.HasScaleInitFillQty) order.ScaleInitFillQty = orderProto.ScaleInitFillQty;
            if (orderProto.HasScaleRandomPercent) order.ScaleRandomPercent = orderProto.ScaleRandomPercent;
            if (orderProto.HasHedgeType) order.HedgeType = orderProto.HedgeType;
            if (orderProto.HasHedgeType && orderProto.HasHedgeParam && !Util.StringIsEmpty(orderProto.HedgeType)) order.HedgeParam = orderProto.HedgeParam;
            if (orderProto.HasOptOutSmartRouting) order.OptOutSmartRouting = orderProto.OptOutSmartRouting;
            if (orderProto.HasClearingAccount) order.ClearingAccount = orderProto.ClearingAccount;
            if (orderProto.HasClearingIntent) order.ClearingIntent = orderProto.ClearingIntent;
            if (orderProto.HasNotHeld) order.NotHeld = orderProto.NotHeld;

            if (orderProto.HasAlgoStrategy) 
            {
                order.AlgoStrategy = orderProto.AlgoStrategy;
                if (orderProto.AlgoParams.Count > 0) 
                {
                    List<TagValue> algoParams = decodeTagValueList(orderProto.AlgoParams);
                    order.AlgoParams = algoParams;
                }
            }

            if (orderProto.HasSolicited) order.Solicited = orderProto.Solicited;
            if (orderProto.HasWhatIf) order.WhatIf = orderProto.WhatIf;
            if (orderProto.HasRandomizeSize) order.RandomizeSize = orderProto.RandomizeSize;
            if (orderProto.HasRandomizePrice) order.RandomizePrice = orderProto.RandomizePrice;
            if (orderProto.HasReferenceContractId) order.ReferenceContractId = orderProto.ReferenceContractId;
            if (orderProto.HasIsPeggedChangeAmountDecrease) order.IsPeggedChangeAmountDecrease = orderProto.IsPeggedChangeAmountDecrease;
            if (orderProto.HasPeggedChangeAmount) order.PeggedChangeAmount = orderProto.PeggedChangeAmount;
            if (orderProto.HasReferenceChangeAmount) order.ReferenceChangeAmount = orderProto.ReferenceChangeAmount;
            if (orderProto.HasReferenceExchangeId) order.ReferenceExchange = orderProto.ReferenceExchangeId;

            List<OrderCondition> conditions = decodeConditions(orderProto);
            if (conditions != null) order.Conditions = conditions;
            if (orderProto.HasConditionsIgnoreRth) order.ConditionsIgnoreRth = orderProto.ConditionsIgnoreRth;
            if (orderProto.HasConditionsCancelOrder) order.ConditionsCancelOrder = orderProto.ConditionsCancelOrder;

            if (orderProto.HasAdjustedOrderType) order.AdjustedOrderType = orderProto.AdjustedOrderType;
            if (orderProto.HasTriggerPrice) order.TriggerPrice = orderProto.TriggerPrice;
            if (orderProto.HasLmtPriceOffset) order.LmtPriceOffset = orderProto.LmtPriceOffset;
            if (orderProto.HasAdjustedStopPrice) order.AdjustedStopPrice = orderProto.AdjustedStopPrice;
            if (orderProto.HasAdjustedStopLimitPrice) order.AdjustedStopLimitPrice = orderProto.AdjustedStopLimitPrice;
            if (orderProto.HasAdjustedTrailingAmount) order.AdjustedTrailingAmount = orderProto.AdjustedTrailingAmount;
            if (orderProto.HasAdjustableTrailingUnit) order.AdjustableTrailingUnit = orderProto.AdjustableTrailingUnit;

            order.Tier = decodeSoftDollarTier(orderProto);

            if (orderProto.HasCashQty) order.CashQty = orderProto.CashQty;
            if (orderProto.HasDontUseAutoPriceForHedge) order.DontUseAutoPriceForHedge = orderProto.DontUseAutoPriceForHedge;
            if (orderProto.HasIsOmsContainer) order.IsOmsContainer = orderProto.IsOmsContainer;
            if (orderProto.HasDiscretionaryUpToLimitPrice) order.DiscretionaryUpToLimitPrice = orderProto.DiscretionaryUpToLimitPrice;
            if (orderProto.HasUsePriceMgmtAlgo) order.UsePriceMgmtAlgo = orderProto.UsePriceMgmtAlgo != 0;
            if (orderProto.HasDuration) order.Duration = orderProto.Duration;
            if (orderProto.HasPostToAts) order.PostToAts = orderProto.PostToAts;
            if (orderProto.HasAutoCancelParent) order.AutoCancelParent = orderProto.AutoCancelParent;
            if (orderProto.HasMinTradeQty) order.MinTradeQty = orderProto.MinTradeQty;
            if (orderProto.HasMinCompeteSize) order.MinCompeteSize = orderProto.MinCompeteSize;
            if (orderProto.HasCompeteAgainstBestOffset) order.CompeteAgainstBestOffset = orderProto.CompeteAgainstBestOffset;
            if (orderProto.HasMidOffsetAtWhole) order.MidOffsetAtWhole = orderProto.MidOffsetAtWhole;
            if (orderProto.HasMidOffsetAtHalf) order.MidOffsetAtHalf = orderProto.MidOffsetAtHalf;
            if (orderProto.HasCustomerAccount) order.CustomerAccount = orderProto.CustomerAccount;
            if (orderProto.HasProfessionalCustomer) order.ProfessionalCustomer = orderProto.ProfessionalCustomer;
            if (orderProto.HasBondAccruedInterest) order.BondAccruedInterest = orderProto.BondAccruedInterest;
            if (orderProto.HasIncludeOvernight) order.IncludeOvernight = orderProto.IncludeOvernight;
            if (orderProto.HasExtOperator) order.ExtOperator = orderProto.ExtOperator;
            if (orderProto.HasManualOrderIndicator) order.ManualOrderIndicator = orderProto.ManualOrderIndicator;
            if (orderProto.HasSubmitter) order.Submitter = orderProto.Submitter;
            if (orderProto.HasImbalanceOnly) order.ImbalanceOnly = orderProto.ImbalanceOnly;
            if (orderProto.HasAutoCancelDate) order.AutoCancelDate = orderProto.AutoCancelDate;
            if (orderProto.HasFilledQuantity) order.FilledQuantity = Util.StringToDecimal(orderProto.FilledQuantity);
            if (orderProto.HasRefFuturesConId) order.RefFuturesConId = orderProto.RefFuturesConId;
            if (orderProto.HasShareholder) order.Shareholder = orderProto.Shareholder;
            if (orderProto.HasRouteMarketableToBbo) order.RouteMarketableToBbo = orderProto.RouteMarketableToBbo;
            if (orderProto.HasParentPermId) order.ParentPermId = orderProto.ParentPermId;

            return order;
        }

        public static List<OrderCondition> decodeConditions(protobuf.Order order) 
        {
            List<OrderCondition> orderConditions = null;

            if (order.Conditions.Count > 0) {
                orderConditions = new List<OrderCondition>();
                foreach (protobuf.OrderCondition orderConditionProto in order.Conditions) 
                {
                    int conditionTypeValue = orderConditionProto.HasType ? orderConditionProto.Type : 0;
                    OrderConditionType conditionType = (OrderConditionType)conditionTypeValue;

                    OrderCondition orderCondition = null;
                    switch(conditionType) {
                        case OrderConditionType.Price:
                            orderCondition = createPriceCondition(orderConditionProto);
                            break;
                        case OrderConditionType.Time:
                            orderCondition = createTimeCondition(orderConditionProto);
                            break;
                        case OrderConditionType.Margin:
                            orderCondition = createMarginCondition(orderConditionProto);
                            break;
                        case OrderConditionType.Execution:
                            orderCondition = createExecutionCondition(orderConditionProto);
                            break;
                        case OrderConditionType.Volume:
                            orderCondition = createVolumeCondition(orderConditionProto);
                            break;
                        case OrderConditionType.PercentCange:
                            orderCondition = createPercentChangeCondition(orderConditionProto);
                            break;
                    }
                    if (orderCondition != null) {
                        orderConditions.Add(orderCondition);
                    }
                }
            }

            return orderConditions;
        }

        private static void setConditionFields(protobuf.OrderCondition orderConditionProto, OrderCondition orderCondition) 
        {
            if (orderConditionProto.HasIsConjunctionConnection) orderCondition.IsConjunctionConnection = orderConditionProto.IsConjunctionConnection;
        }

        private static void setOperatorConditionFields(protobuf.OrderCondition orderConditionProto, OperatorCondition operatorCondition) 
        {
            setConditionFields(orderConditionProto, operatorCondition);
            if (orderConditionProto.HasIsMore) operatorCondition.IsMore = orderConditionProto.IsMore;
        }

        private static void setContractConditionFields(protobuf.OrderCondition orderConditionProto, ContractCondition contractCondition) 
        {
            setOperatorConditionFields(orderConditionProto, contractCondition);
            if (orderConditionProto.HasConId) contractCondition.ConId = orderConditionProto.ConId;
            if (orderConditionProto.HasExchange) contractCondition.Exchange = orderConditionProto.Exchange;
        }

        private static PriceCondition createPriceCondition(protobuf.OrderCondition orderConditionProto) 
        {
            PriceCondition priceCondition = (PriceCondition)OrderCondition.Create(OrderConditionType.Price);
            setContractConditionFields(orderConditionProto, priceCondition);
            if (orderConditionProto.HasPrice) priceCondition.Price = orderConditionProto.Price;
            if (orderConditionProto.HasTriggerMethod) priceCondition.TriggerMethod = (TriggerMethod)orderConditionProto.TriggerMethod;
            return priceCondition;
        }

        private static TimeCondition createTimeCondition(protobuf.OrderCondition orderConditionProto) 
        {
            TimeCondition timeCondition = (TimeCondition)OrderCondition.Create(OrderConditionType.Time);
            setOperatorConditionFields(orderConditionProto, timeCondition);
            if (orderConditionProto.HasTime) timeCondition.Time = orderConditionProto.Time;
            return timeCondition;
        }

        private static MarginCondition createMarginCondition(protobuf.OrderCondition orderConditionProto) 
        {
            MarginCondition marginCondition = (MarginCondition)OrderCondition.Create(OrderConditionType.Margin);
            setOperatorConditionFields(orderConditionProto, marginCondition);
            if (orderConditionProto.HasPercent) marginCondition.Percent = orderConditionProto.Percent;
            return marginCondition;
        }

        private static ExecutionCondition createExecutionCondition(protobuf.OrderCondition orderConditionProto) 
        {
            ExecutionCondition executionCondition = (ExecutionCondition)OrderCondition.Create(OrderConditionType.Execution);
            setConditionFields(orderConditionProto, executionCondition);
            if (orderConditionProto.HasSecType) executionCondition.SecType = orderConditionProto.SecType;
            if (orderConditionProto.HasExchange) executionCondition.Exchange = orderConditionProto.Exchange;
            if (orderConditionProto.HasSymbol) executionCondition.Symbol = orderConditionProto.Symbol;
            return executionCondition;
        }

        private static VolumeCondition createVolumeCondition(protobuf.OrderCondition orderConditionProto) {
            VolumeCondition volumeCondition = (VolumeCondition)OrderCondition.Create(OrderConditionType.Volume);
            setContractConditionFields(orderConditionProto, volumeCondition);
            if (orderConditionProto.HasVolume) volumeCondition.Volume = orderConditionProto.Volume;
            return volumeCondition;
        }

        private static PercentChangeCondition createPercentChangeCondition(protobuf.OrderCondition orderConditionProto) {
            PercentChangeCondition percentChangeCondition = (PercentChangeCondition)OrderCondition.Create(OrderConditionType.PercentCange);
            setContractConditionFields(orderConditionProto, percentChangeCondition);
            if (orderConditionProto.HasChangePercent) percentChangeCondition.ChangePercent = orderConditionProto.ChangePercent;
            return percentChangeCondition;
         }

        public static SoftDollarTier decodeSoftDollarTier(protobuf.Order order) {
            protobuf.SoftDollarTier softDollarTierProto = order.SoftDollarTier;
            string name = "";
            string value = "";
            string displayName = "";
            if (softDollarTierProto != null) {
                if (softDollarTierProto.HasName) name = softDollarTierProto.Name;
                if (softDollarTierProto.HasValue) value = softDollarTierProto.Value;
                if (softDollarTierProto.HasDisplayName) displayName = softDollarTierProto.DisplayName;
            }
            return new SoftDollarTier(name, value, displayName);
        }

        public static List<TagValue> decodeTagValueList(MapField<string, string> stringStringMap)
        {
            List<TagValue> tagValueList = new List<TagValue>();
            int paramsCount = stringStringMap.Count;
            if (paramsCount > 0)
            {
                foreach (var item in stringStringMap)
                {
                    tagValueList.Add(new TagValue(item.Key, item.Value));
                }
            }
            return tagValueList;
        }

        public static OrderState decodeOrderState(protobuf.OrderState orderStateProto) {
            OrderState orderState = new OrderState();
            if (orderStateProto.HasStatus) orderState.Status = orderStateProto.Status;
            if (orderStateProto.HasInitMarginBefore) orderState.InitMarginBefore = orderStateProto.InitMarginBefore.ToString();
            if (orderStateProto.HasMaintMarginBefore) orderState.MaintMarginBefore = orderStateProto.MaintMarginBefore.ToString();
            if (orderStateProto.HasEquityWithLoanBefore) orderState.EquityWithLoanBefore = orderStateProto.EquityWithLoanBefore.ToString();
            if (orderStateProto.HasInitMarginChange) orderState.InitMarginChange = orderStateProto.InitMarginChange.ToString();
            if (orderStateProto.HasMaintMarginChange) orderState.MaintMarginChange = orderStateProto.MaintMarginChange.ToString();
            if (orderStateProto.HasEquityWithLoanChange) orderState.EquityWithLoanChange = orderStateProto.EquityWithLoanChange.ToString();
            if (orderStateProto.HasInitMarginAfter) orderState.InitMarginAfter = orderStateProto.InitMarginAfter.ToString();
            if (orderStateProto.HasMaintMarginAfter) orderState.MaintMarginAfter = orderStateProto.MaintMarginAfter.ToString();
            if (orderStateProto.HasEquityWithLoanAfter) orderState.EquityWithLoanAfter = orderStateProto.EquityWithLoanAfter.ToString();
            if (orderStateProto.HasCommissionAndFees) orderState.CommissionAndFees = orderStateProto.CommissionAndFees;
            if (orderStateProto.HasMinCommissionAndFees) orderState.MinCommissionAndFees = orderStateProto.MinCommissionAndFees;
            if (orderStateProto.HasMaxCommissionAndFees) orderState.MaxCommissionAndFees = orderStateProto.MaxCommissionAndFees;
            if (orderStateProto.HasCommissionAndFeesCurrency) orderState.CommissionAndFeesCurrency = orderStateProto.CommissionAndFeesCurrency;
            if (orderStateProto.HasWarningText) orderState.WarningText = orderStateProto.WarningText;
            if (orderStateProto.HasMarginCurrency) orderState.MarginCurrency = orderStateProto.MarginCurrency;
            if (orderStateProto.HasInitMarginBeforeOutsideRTH) orderState.InitMarginBeforeOutsideRTH = orderStateProto.InitMarginBeforeOutsideRTH;
            if (orderStateProto.HasMaintMarginBeforeOutsideRTH) orderState.MaintMarginBeforeOutsideRTH = orderStateProto.MaintMarginBeforeOutsideRTH;
            if (orderStateProto.HasEquityWithLoanBeforeOutsideRTH) orderState.EquityWithLoanBeforeOutsideRTH = orderStateProto.EquityWithLoanBeforeOutsideRTH;
            if (orderStateProto.HasInitMarginChangeOutsideRTH) orderState.InitMarginChangeOutsideRTH = orderStateProto.InitMarginChangeOutsideRTH;
            if (orderStateProto.HasMaintMarginChangeOutsideRTH) orderState.MaintMarginChangeOutsideRTH = orderStateProto.MaintMarginChangeOutsideRTH;
            if (orderStateProto.HasEquityWithLoanChangeOutsideRTH) orderState.EquityWithLoanChangeOutsideRTH = orderStateProto.EquityWithLoanChangeOutsideRTH;
            if (orderStateProto.HasInitMarginAfterOutsideRTH) orderState.InitMarginAfterOutsideRTH = orderStateProto.InitMarginAfterOutsideRTH;
            if (orderStateProto.HasMaintMarginAfterOutsideRTH) orderState.MaintMarginAfterOutsideRTH = orderStateProto.MaintMarginAfterOutsideRTH;
            if (orderStateProto.HasEquityWithLoanAfterOutsideRTH) orderState.EquityWithLoanAfterOutsideRTH = orderStateProto.EquityWithLoanAfterOutsideRTH;
            if (orderStateProto.HasSuggestedSize) orderState.SuggestedSize = Util.StringToDecimal(orderStateProto.SuggestedSize);
            if (orderStateProto.HasRejectReason) orderState.RejectReason = orderStateProto.RejectReason;

            List<OrderAllocation> orderAllocations = decodeOrderAllocations(orderStateProto);
            if (orderAllocations != null) orderState.OrderAllocations = orderAllocations;

            if (orderStateProto.HasCompletedTime) orderState.CompletedTime = orderStateProto.CompletedTime;
            if (orderStateProto.HasCompletedStatus) orderState.CompletedStatus = orderStateProto.CompletedStatus;

            return orderState;
        }

        public static List<OrderAllocation> decodeOrderAllocations(protobuf.OrderState orderStateProto) {
            List<OrderAllocation> orderAllocations = null;
            if (orderStateProto.OrderAllocations.Count > 0) {
                orderAllocations = new List<OrderAllocation>();
                foreach (protobuf.OrderAllocation orderAllocationProto in orderStateProto.OrderAllocations) {
                    OrderAllocation orderAllocation = new OrderAllocation();

                    if (orderAllocationProto.HasAccount) orderAllocation.Account = orderAllocationProto.Account;
                    if (orderAllocationProto.HasPosition) orderAllocation.Position = Util.StringToDecimal(orderAllocationProto.Position);
                    if (orderAllocationProto.HasPositionDesired) orderAllocation.PositionDesired = Util.StringToDecimal(orderAllocationProto.PositionDesired);
                    if (orderAllocationProto.HasPositionAfter) orderAllocation.PositionAfter = Util.StringToDecimal(orderAllocationProto.PositionAfter);
                    if (orderAllocationProto.HasDesiredAllocQty) orderAllocation.DesiredAllocQty = Util.StringToDecimal(orderAllocationProto.DesiredAllocQty);
                    if (orderAllocationProto.HasAllowedAllocQty) orderAllocation.AllowedAllocQty = Util.StringToDecimal(orderAllocationProto.AllowedAllocQty);
                    if (orderAllocationProto.HasIsMonetary) orderAllocation.IsMonetary = orderAllocationProto.IsMonetary;
                    orderAllocations.Add(orderAllocation);
                }
            }
            return orderAllocations;
        }

        public static ContractDetails decodeContractDetails(protobuf.Contract contractProto, protobuf.ContractDetails contractDetailsProto, bool isBond)
        {
            ContractDetails contractDetails = new ContractDetails();
            Contract contract = decodeContract(contractProto);
            if (contract != null) contractDetails.Contract = contract;

            if (contractDetailsProto.HasMarketName) contractDetails.MarketName = contractDetailsProto.MarketName;
            if (contractDetailsProto.HasMinTick) contractDetails.MinTick = Util.StringToDoubleMax(contractDetailsProto.MinTick);
            if (contractDetailsProto.HasPriceMagnifier) contractDetails.PriceMagnifier = contractDetailsProto.PriceMagnifier;
            if (contractDetailsProto.HasOrderTypes) contractDetails.OrderTypes = contractDetailsProto.OrderTypes;
            if (contractDetailsProto.HasValidExchanges) contractDetails.ValidExchanges = contractDetailsProto.ValidExchanges;
            if (contractDetailsProto.HasUnderConId) contractDetails.UnderConId = contractDetailsProto.UnderConId;
            if (contractDetailsProto.HasLongName) contractDetails.LongName = contractDetailsProto.LongName;
            if (contractDetailsProto.HasContractMonth) contractDetails.ContractMonth = contractDetailsProto.ContractMonth;
            if (contractDetailsProto.HasIndustry) contractDetails.Industry = contractDetailsProto.Industry;
            if (contractDetailsProto.HasCategory) contractDetails.Category = contractDetailsProto.Category;
            if (contractDetailsProto.HasSubcategory) contractDetails.Subcategory = contractDetailsProto.Subcategory;
            if (contractDetailsProto.HasTimeZoneId) contractDetails.TimeZoneId = contractDetailsProto.TimeZoneId;
            if (contractDetailsProto.HasTradingHours) contractDetails.TradingHours = contractDetailsProto.TradingHours;
            if (contractDetailsProto.HasLiquidHours) contractDetails.LiquidHours = contractDetailsProto.LiquidHours;
            if (contractDetailsProto.HasEvRule) contractDetails.EvRule = contractDetailsProto.EvRule;
            if (contractDetailsProto.HasEvMultiplier) contractDetails.EvMultiplier = contractDetailsProto.EvMultiplier;

            if (contractDetailsProto.SecIdList.Count > 0)
            {
                List<TagValue> secIdList = decodeTagValueList(contractDetailsProto.SecIdList);
                contractDetails.SecIdList = secIdList;
            }

            if (contractDetailsProto.HasAggGroup) contractDetails.AggGroup = contractDetailsProto.AggGroup;
            if (contractDetailsProto.HasUnderSymbol) contractDetails.UnderSymbol = contractDetailsProto.UnderSymbol;
            if (contractDetailsProto.HasUnderSecType) contractDetails.UnderSecType = contractDetailsProto.UnderSecType;
            if (contractDetailsProto.HasMarketRuleIds) contractDetails.MarketRuleIds = contractDetailsProto.MarketRuleIds;
            if (contractDetailsProto.HasRealExpirationDate) contractDetails.RealExpirationDate = contractDetailsProto.RealExpirationDate;
            if (contractDetailsProto.HasStockType) contractDetails.StockType = contractDetailsProto.StockType;
            if (contractDetailsProto.HasMinSize) contractDetails.MinSize = Util.StringToDecimal(contractDetailsProto.MinSize);
            if (contractDetailsProto.HasSizeIncrement) contractDetails.SizeIncrement = Util.StringToDecimal(contractDetailsProto.SizeIncrement);
            if (contractDetailsProto.HasSuggestedSizeIncrement) contractDetails.SuggestedSizeIncrement = Util.StringToDecimal(contractDetailsProto.SuggestedSizeIncrement);

            SetLastTradeDate(contract.LastTradeDateOrContractMonth, contractDetails, isBond);

            if (contractDetailsProto.HasCusip) contractDetails.Cusip = contractDetailsProto.Cusip;
            if (contractDetailsProto.HasRatings) contractDetails.Ratings = contractDetailsProto.Ratings;
            if (contractDetailsProto.HasDescAppend) contractDetails.DescAppend = contractDetailsProto.DescAppend;
            if (contractDetailsProto.HasBondType) contractDetails.BondType = contractDetailsProto.BondType;
            if (contractDetailsProto.HasCoupon) contractDetails.Coupon = contractDetailsProto.Coupon;
            if (contractDetailsProto.HasCouponType) contractDetails.CouponType = contractDetailsProto.CouponType;
            if (contractDetailsProto.HasCallable) contractDetails.Callable = contractDetailsProto.Callable;
            if (contractDetailsProto.HasPuttable) contractDetails.Putable = contractDetailsProto.Puttable;
            if (contractDetailsProto.HasConvertible) contractDetails.Convertible = contractDetailsProto.Convertible;
            if (contractDetailsProto.HasIssueDate) contractDetails.IssueDate = contractDetailsProto.IssueDate;
            if (contractDetailsProto.HasNextOptionDate) contractDetails.NextOptionDate = contractDetailsProto.NextOptionDate;
            if (contractDetailsProto.HasNextOptionType) contractDetails.NextOptionType = contractDetailsProto.NextOptionType;
            if (contractDetailsProto.HasNextOptionPartial) contractDetails.NextOptionPartial = contractDetailsProto.NextOptionPartial;
            if (contractDetailsProto.HasBondNotes) contractDetails.Notes = contractDetailsProto.BondNotes;

            if (contractDetailsProto.HasFundName) contractDetails.FundName = contractDetailsProto.FundName;
            if (contractDetailsProto.HasFundFamily) contractDetails.FundFamily = contractDetailsProto.FundFamily;
            if (contractDetailsProto.HasFundType) contractDetails.FundType = contractDetailsProto.FundType;
            if (contractDetailsProto.HasFundFrontLoad) contractDetails.FundFrontLoad = contractDetailsProto.FundFrontLoad;
            if (contractDetailsProto.HasFundBackLoad) contractDetails.FundBackLoad = contractDetailsProto.FundBackLoad;
            if (contractDetailsProto.HasFundBackLoadTimeInterval) contractDetails.FundBackLoadTimeInterval = contractDetailsProto.FundBackLoadTimeInterval;
            if (contractDetailsProto.HasFundManagementFee) contractDetails.FundManagementFee = contractDetailsProto.FundManagementFee;
            if (contractDetailsProto.HasFundClosed) contractDetails.FundClosed = contractDetailsProto.FundClosed;
            if (contractDetailsProto.HasFundClosedForNewInvestors) contractDetails.FundClosedForNewInvestors = contractDetailsProto.FundClosedForNewInvestors;
            if (contractDetailsProto.HasFundClosedForNewMoney) contractDetails.FundClosedForNewMoney = contractDetailsProto.FundClosedForNewMoney;
            if (contractDetailsProto.HasFundNotifyAmount) contractDetails.FundNotifyAmount = contractDetailsProto.FundNotifyAmount;
            if (contractDetailsProto.HasFundMinimumInitialPurchase) contractDetails.FundMinimumInitialPurchase = contractDetailsProto.FundMinimumInitialPurchase;
            if (contractDetailsProto.HasFundMinimumSubsequentPurchase) contractDetails.FundSubsequentMinimumPurchase = contractDetailsProto.FundMinimumSubsequentPurchase;
            if (contractDetailsProto.HasFundBlueSkyStates) contractDetails.FundBlueSkyStates = contractDetailsProto.FundBlueSkyStates;
            if (contractDetailsProto.HasFundBlueSkyTerritories) contractDetails.FundBlueSkyTerritories = contractDetailsProto.FundBlueSkyTerritories;
            if (contractDetailsProto.HasFundDistributionPolicyIndicator) contractDetails.FundDistributionPolicyIndicator = CFundDistributionPolicyIndicator.getFundDistributionPolicyIndicator(contractDetailsProto.FundDistributionPolicyIndicator);
            if (contractDetailsProto.HasFundAssetType) contractDetails.FundAssetType = CFundAssetType.getFundAssetType(contractDetailsProto.FundAssetType);

            List<IneligibilityReason> ineligibilityReasonList = decodeIneligibilityReasonList(contractDetailsProto);
            if (ineligibilityReasonList != null && ineligibilityReasonList.Any()) contractDetails.IneligibilityReasonList = ineligibilityReasonList;

            return contractDetails;
        }

        public static List<IneligibilityReason> decodeIneligibilityReasonList(protobuf.ContractDetails contractDetailsProto)
        {
            List<IneligibilityReason> ineligibilityReasonList = null;

            if (contractDetailsProto.IneligibilityReasonList.Count > 0)
            {
                List<protobuf.IneligibilityReason> ineligibilityReasonProtoList = new List<protobuf.IneligibilityReason>();
                ineligibilityReasonProtoList.AddRange(contractDetailsProto.IneligibilityReasonList);
                ineligibilityReasonList = new List<IneligibilityReason>();

                foreach (protobuf.IneligibilityReason ineligibilityReasonProto in ineligibilityReasonProtoList)
                {
                    IneligibilityReason ineligibilityReason = new IneligibilityReason();
                    if (ineligibilityReasonProto.HasId) ineligibilityReason.Id = ineligibilityReasonProto.Id;
                    if (ineligibilityReasonProto.HasDescription) ineligibilityReason.Description = ineligibilityReasonProto.Description;
                    ineligibilityReasonList.Add(ineligibilityReason);
                }
            }
            return ineligibilityReasonList;
        }
        public static void SetLastTradeDate(string lastTradeDateOrContractMonth, ContractDetails contract, bool isBond)
        {
            if (lastTradeDateOrContractMonth != null)
            {
                var split = lastTradeDateOrContractMonth.Contains("-") ? Regex.Split(lastTradeDateOrContractMonth, "-") : Regex.Split(lastTradeDateOrContractMonth, "\\s+");
                if (split.Length > 0)
                {
                    if (isBond) contract.Maturity = split[0];
                    else contract.Contract.LastTradeDateOrContractMonth = split[0];
                }
                if (split.Length > 1) contract.LastTradeTime = split[1];
                if (isBond && split.Length > 2) contract.TimeZoneId = split[2];
            }
        }

    }
}
